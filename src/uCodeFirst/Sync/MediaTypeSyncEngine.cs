using System.Security.Cryptography;
using System.Text;
using uCodeFirst.Attributes;
using uCodeFirst.Discovery;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Strings;

namespace uCodeFirst.Sync;

internal sealed class MediaTypeSyncEngine
{
    private readonly IMediaTypeService _mediaTypeService;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly ILogger<MediaTypeSyncEngine> _logger;

    public MediaTypeSyncEngine(
        IMediaTypeService mediaTypeService,
        IShortStringHelper shortStringHelper,
        ILogger<MediaTypeSyncEngine> logger)
    {
        _mediaTypeService = mediaTypeService;
        _shortStringHelper = shortStringHelper;
        _logger = logger;
    }

    public async Task SyncAsync(
        IReadOnlyList<MediaTypeDefinition> definitions,
        Dictionary<Guid, IDataType> dataTypeByKey,
        CancellationToken ct = default)
    {
        var aliasByKey = definitions.ToDictionary(d => d.Key, d => d.Alias);
        var folderIdByPath = await EnsureFoldersAsync(definitions);

        // Pass 1: create/update all media types
        foreach (var def in definitions)
        {
            var existing = await _mediaTypeService.GetAsync(def.Key);
            if (existing is null)
                await CreateAsync(def, dataTypeByKey, folderIdByPath);
            else
                await UpdateAsync(existing, def, dataTypeByKey, folderIdByPath);
        }

        // Pass 2: wire AllowedChildren
        foreach (var def in definitions)
        {
            if (def.AllowedChildTypes.Count == 0)
                continue;

            var mediaType = await _mediaTypeService.GetAsync(def.Key);
            if (mediaType is null)
                continue;

            ApplyAllowedChildren(mediaType, def, aliasByKey);
            var result = await _mediaTypeService.UpdateAsync(mediaType, Constants.Security.SuperUserKey);
            if (!result.Success)
                _logger.LogError("Failed to set AllowedChildren on media type '{Alias}': {Status}.", def.Alias, result.Result);
        }

        // Pass 3: wire external compositions
        await SyncCompositionsAsync(definitions, ct);
    }

    private async Task SyncCompositionsAsync(IReadOnlyList<MediaTypeDefinition> definitions, CancellationToken ct)
    {
        var defsWithCompositions = definitions.Where(d => d.CompositionKeys.Count > 0).ToList();
        if (defsWithCompositions.Count == 0)
            return;

        foreach (var def in defsWithCompositions)
        {
            var mediaType = await _mediaTypeService.GetAsync(def.Key);
            if (mediaType is null)
                continue;

            var changed = false;
            foreach (var compKey in def.CompositionKeys)
            {
                var compType = await _mediaTypeService.GetAsync(compKey);
                if (compType is not null && mediaType.AddContentType(compType))
                    changed = true;
                else if (compType is null)
                    _logger.LogWarning("Composition media type '{Key}' not found — skipping for '{Alias}'.", compKey, def.Alias);
            }

            if (changed)
            {
                var result = await _mediaTypeService.UpdateAsync(mediaType, Constants.Security.SuperUserKey);
                if (!result.Success)
                    _logger.LogError("Failed to set compositions on media type '{Alias}': {Status}.", def.Alias, result.Result);
                else
                    _logger.LogInformation("Wired compositions on media type '{Alias}'.", def.Alias);
            }
        }
    }

    private async Task<Dictionary<string, int>> EnsureFoldersAsync(IReadOnlyList<MediaTypeDefinition> definitions)
    {
        var folderIdByPath = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var paths = definitions
            .Where(d => d.Folder is not null)
            .Select(d => d.Folder!)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var folderPath in paths)
        {
            var segments = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var builtPath = string.Empty;
            var parentId = -1;

            foreach (var segment in segments)
            {
                builtPath = builtPath.Length == 0 ? segment : $"{builtPath}/{segment}";

                if (folderIdByPath.TryGetValue(builtPath, out var existingId))
                {
                    parentId = existingId;
                    continue;
                }

                var folderKey = DeterministicFolderKey(builtPath);
                var existing = _mediaTypeService.GetContainer(folderKey);

                if (existing is not null)
                {
                    folderIdByPath[builtPath] = existing.Id;
                    parentId = existing.Id;
                    continue;
                }

                var result = _mediaTypeService.CreateContainer(parentId, folderKey, segment, Constants.Security.SuperUserId);
                if (result.Success && result.Result?.Entity is not null)
                {
                    folderIdByPath[builtPath] = result.Result.Entity.Id;
                    parentId = result.Result.Entity.Id;
                    _logger.LogInformation("Created media type folder '{Path}'.", builtPath);
                }
                else
                {
                    _logger.LogError("Failed to create media type folder '{Path}'.", builtPath);
                }
            }
        }

        return await Task.FromResult(folderIdByPath);
    }

    private static Guid DeterministicFolderKey(string folderPath)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes($"consid.codefirst:mediafolder:{folderPath.ToLowerInvariant()}"));
        return new Guid(hash);
    }

    private async Task CreateAsync(
        MediaTypeDefinition def,
        Dictionary<Guid, IDataType> dataTypeByKey,
        Dictionary<string, int> folderIdByPath)
    {
        var parentId = def.Folder is not null && folderIdByPath.TryGetValue(def.Folder, out var fId) ? fId : -1;

        var mediaType = new MediaType(_shortStringHelper, parentId: parentId)
        {
            Key = def.Key,
            Alias = def.Alias,
            Name = def.Name,
            Icon = BuildIconString(def.Icon, def.Color),
            Description = def.Description ?? string.Empty,
            AllowedAsRoot = def.AllowedAtRoot
        };

        ApplyProperties(mediaType, def, dataTypeByKey);

        var result = await _mediaTypeService.CreateAsync(mediaType, Constants.Security.SuperUserKey);
        if (result.Success)
            _logger.LogInformation("Created media type '{Alias}' ({Key}).", def.Alias, def.Key);
        else
            _logger.LogError("Failed to create media type '{Alias}': {Status}.", def.Alias, result.Result);
    }

    private async Task UpdateAsync(
        IMediaType existing,
        MediaTypeDefinition def,
        Dictionary<Guid, IDataType> dataTypeByKey,
        Dictionary<string, int> folderIdByPath)
    {
        existing.Alias = def.Alias;
        existing.Name = def.Name;
        existing.Icon = BuildIconString(def.Icon, def.Color);
        existing.Description = def.Description ?? string.Empty;
        existing.AllowedAsRoot = def.AllowedAtRoot;

        var targetParentId = def.Folder is not null && folderIdByPath.TryGetValue(def.Folder, out var fId) ? fId : -1;
        if (existing.ParentId != targetParentId)
            existing.ParentId = targetParentId;

        MergeProperties(existing, def, dataTypeByKey);

        var result = await _mediaTypeService.UpdateAsync(existing, Constants.Security.SuperUserKey);
        if (result.Success)
            _logger.LogInformation("Updated media type '{Alias}' ({Key}).", def.Alias, def.Key);
        else
            _logger.LogError("Failed to update media type '{Alias}': {Status}.", def.Alias, result.Result);
    }

    private void ApplyProperties(IMediaType mediaType, MediaTypeDefinition def, Dictionary<Guid, IDataType> dataTypeByKey)
    {
        var groupedProps = def.Properties
            .GroupBy(p => p.GroupName)
            .Select((g, i) => (Group: g, Index: i))
            .ToList();

        foreach (var (group, groupIndex) in groupedProps)
        {
            var groupAlias = DocumentTypeScanner.ToAlias(group.Key);

            var propertyGroup = new PropertyGroup(isPublishing: true)
            {
                Alias = groupAlias,
                Name = group.Key,
                Type = PropertyGroupType.Tab,
                SortOrder = groupIndex
            };
            mediaType.PropertyGroups.Add(propertyGroup);

            foreach (var prop in group)
            {
                if (!dataTypeByKey.TryGetValue(prop.DataType.GetDescriptor().Key, out var dataType))
                {
                    _logger.LogWarning("Data type for property '{Alias}' on '{Type}' not found — skipping.", prop.Alias, def.ClrType.Name);
                    continue;
                }

                var propertyType = new PropertyType(_shortStringHelper, dataType, prop.Alias)
                {
                    Name = prop.Name,
                    Mandatory = prop.Mandatory,
                    Description = prop.Description ?? string.Empty,
                    SortOrder = prop.SortOrder
                };

                mediaType.AddPropertyType(propertyType, groupAlias, group.Key);
            }
        }
    }

    private void MergeProperties(IMediaType mediaType, MediaTypeDefinition def, Dictionary<Guid, IDataType> dataTypeByKey)
    {
        var groupedProps = def.Properties
            .GroupBy(p => p.GroupName)
            .Select((g, i) => (Group: g, Index: i))
            .ToList();

        foreach (var (group, groupIndex) in groupedProps)
        {
            var groupAlias = DocumentTypeScanner.ToAlias(group.Key);

            var propertyGroup = mediaType.PropertyGroups
                .FirstOrDefault(g => string.Equals(g.Alias, groupAlias, StringComparison.OrdinalIgnoreCase));

            if (propertyGroup is null)
            {
                propertyGroup = new PropertyGroup(isPublishing: true)
                {
                    Alias = groupAlias,
                    Name = group.Key,
                    Type = PropertyGroupType.Tab,
                    SortOrder = groupIndex
                };
                mediaType.PropertyGroups.Add(propertyGroup);
            }
            else
            {
                propertyGroup.Name = group.Key;
                propertyGroup.SortOrder = groupIndex;
            }

            foreach (var prop in group)
            {
                if (!dataTypeByKey.TryGetValue(prop.DataType.GetDescriptor().Key, out var dataType))
                {
                    _logger.LogWarning("Data type for property '{Alias}' on '{Type}' not found — skipping.", prop.Alias, def.ClrType.Name);
                    continue;
                }

                var existingProp = mediaType.PropertyTypes
                    .FirstOrDefault(pt => string.Equals(pt.Alias, prop.Alias, StringComparison.OrdinalIgnoreCase));

                if (existingProp is not null)
                {
                    existingProp.Name = prop.Name;
                    existingProp.Mandatory = prop.Mandatory;
                    existingProp.Description = prop.Description ?? string.Empty;
                    existingProp.SortOrder = prop.SortOrder;
                    existingProp.DataTypeKey = dataType.Key;
                }
                else
                {
                    var propertyType = new PropertyType(_shortStringHelper, dataType, prop.Alias)
                    {
                        Name = prop.Name,
                        Mandatory = prop.Mandatory,
                        Description = prop.Description ?? string.Empty,
                        SortOrder = prop.SortOrder
                    };
                    mediaType.AddPropertyType(propertyType, groupAlias, group.Key);
                }
            }
        }
    }

    private static void ApplyAllowedChildren(IMediaType mediaType, MediaTypeDefinition def, Dictionary<Guid, string> aliasByKey)
    {
        var sorts = new List<ContentTypeSort>();

        for (var i = 0; i < def.AllowedChildTypes.Count; i++)
        {
            var childType = def.AllowedChildTypes[i];
            var childAttr = (MediaTypeAttribute?)Attribute.GetCustomAttribute(childType, typeof(MediaTypeAttribute));
            if (childAttr is null)
                continue;

            var alias = aliasByKey.GetValueOrDefault(childAttr.Key, childAttr.Alias ?? DocumentTypeScanner.ToAlias(childType.Name));
            sorts.Add(new ContentTypeSort(childAttr.Key, i, alias));
        }

        mediaType.AllowedContentTypes = sorts;
    }

    private static string BuildIconString(string? icon, string? color)
    {
        var base_ = icon ?? "icon-picture";
        return color is not null ? $"{base_} {color}" : base_;
    }
}
