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

internal sealed class ContentTypeSyncEngine
{
    private readonly IContentTypeService _contentTypeService;
    private readonly ITemplateService _templateService;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly ILogger<ContentTypeSyncEngine> _logger;

    public ContentTypeSyncEngine(
        IContentTypeService contentTypeService,
        ITemplateService templateService,
        IShortStringHelper shortStringHelper,
        ILogger<ContentTypeSyncEngine> logger)
    {
        _contentTypeService = contentTypeService;
        _templateService = templateService;
        _shortStringHelper = shortStringHelper;
        _logger = logger;
    }

    public async Task SyncAsync(
        IReadOnlyList<DocumentTypeDefinition> definitions,
        Dictionary<Guid, IDataType> dataTypeByKey,
        CancellationToken ct = default)
    {
        var aliasByKey = definitions.ToDictionary(d => d.Key, d => d.Alias);

        // Keys of all composition types — used to clean up stale compositions on update
        var compositionTypeKeys = definitions
            .Where(d => d.ClrType.IsInterface)
            .Select(d => d.Key)
            .ToHashSet();

        // Pre-pass: ensure all referenced folders exist; build path → int-id map
        var folderIdByPath = await EnsureFoldersAsync(definitions);

        // Pass 1: create/update all types (document types, element types, composition types)
        foreach (var def in definitions)
        {
            var existing = await _contentTypeService.GetAsync(def.Key);
            if (existing is null)
                await CreateAsync(def, dataTypeByKey, folderIdByPath);
            else
                await UpdateAsync(existing, def, dataTypeByKey, folderIdByPath);
        }

        // Pass 2: wire AllowedChildren (document types only)
        foreach (var def in definitions)
        {
            if (def.AllowedChildTypes.Count == 0)
                continue;

            var contentType = await _contentTypeService.GetAsync(def.Key);
            if (contentType is null)
                continue;

            ApplyAllowedChildren(contentType, def, aliasByKey);
            var result = await _contentTypeService.UpdateAsync(contentType, Constants.Security.SuperUserKey);
            if (!result.Success)
                _logger.LogError("Failed to set AllowedChildren on '{Alias}': {Status}.", def.Alias, result.Result);
        }

        // Pass 3: wire compositions
        await SyncCompositionsAsync(definitions, compositionTypeKeys, ct);
    }

    private async Task SyncCompositionsAsync(
        IReadOnlyList<DocumentTypeDefinition> definitions,
        HashSet<Guid> compositionTypeKeys,
        CancellationToken ct)
    {
        var defsWithCompositions = definitions.Where(d => d.CompositionKeys.Count > 0).ToList();
        if (defsWithCompositions.Count == 0)
            return;

        foreach (var def in defsWithCompositions)
        {
            var contentType = await _contentTypeService.GetAsync(def.Key);
            if (contentType is null)
                continue;

            // Remove stale code-first compositions (managed by us but no longer in CompositionKeys)
            var staleKeys = contentType.ContentTypeComposition
                .Where(c => compositionTypeKeys.Contains(c.Key) && !def.CompositionKeys.Contains(c.Key))
                .Select(c => c.Key)
                .ToList();

            var changed = staleKeys.Count > 0;
            foreach (var staleKey in staleKeys)
                contentType.RemoveContentType(staleKey);

            // Add missing compositions
            foreach (var compKey in def.CompositionKeys)
            {
                var compType = await _contentTypeService.GetAsync(compKey);
                if (compType is not null && contentType.AddContentType(compType))
                    changed = true;
            }

            if (changed)
            {
                var result = await _contentTypeService.UpdateAsync(contentType, Constants.Security.SuperUserKey);
                if (!result.Success)
                    _logger.LogError("Failed to set compositions on '{Alias}': {Status}.", def.Alias, result.Result);
                else
                    _logger.LogInformation("Wired compositions on '{Alias}'.", def.Alias);
            }
        }
    }

    // Returns a dictionary from normalised folder path (e.g. "Pages/Articles") → container int id.
    private async Task<Dictionary<string, int>> EnsureFoldersAsync(IReadOnlyList<DocumentTypeDefinition> definitions)
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
                var existing = _contentTypeService.GetContainer(folderKey);

                if (existing is not null)
                {
                    folderIdByPath[builtPath] = existing.Id;
                    parentId = existing.Id;
                    continue;
                }

                var result = _contentTypeService.CreateContainer(parentId, folderKey, segment, Constants.Security.SuperUserId);
                if (result.Success && result.Result?.Entity is not null)
                {
                    folderIdByPath[builtPath] = result.Result.Entity.Id;
                    parentId = result.Result.Entity.Id;
                    _logger.LogInformation("Created document type folder '{Path}'.", builtPath);
                }
                else
                {
                    _logger.LogError("Failed to create document type folder '{Path}'.", builtPath);
                }
            }
        }

        return await Task.FromResult(folderIdByPath);
    }

    private static Guid DeterministicFolderKey(string folderPath)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes($"consid.codefirst:folder:{folderPath.ToLowerInvariant()}"));
        return new Guid(hash);
    }

    private async Task CreateAsync(
        DocumentTypeDefinition def,
        Dictionary<Guid, IDataType> dataTypeByKey,
        Dictionary<string, int> folderIdByPath)
    {
        var parentId = def.Folder is not null && folderIdByPath.TryGetValue(def.Folder, out var fId) ? fId : -1;

        var contentType = new ContentType(_shortStringHelper, parentId: parentId)
        {
            Key = def.Key,
            Alias = def.Alias,
            Name = def.Name,
            Icon = def.Icon ?? "icon-document",
            Description = def.Description ?? string.Empty,
            AllowedAsRoot = def.AllowedAtRoot,
            IsElement = def.IsElement
        };

        ApplyProperties(contentType, def, dataTypeByKey);
        await ApplyTemplateAsync(contentType, def);

        var result = await _contentTypeService.CreateAsync(contentType, Constants.Security.SuperUserKey);
        if (result.Success)
            _logger.LogInformation("Created content type '{Alias}' ({Key}).", def.Alias, def.Key);
        else
            _logger.LogError("Failed to create content type '{Alias}': {Status}.", def.Alias, result.Result);
    }

    private async Task UpdateAsync(
        IContentType existing,
        DocumentTypeDefinition def,
        Dictionary<Guid, IDataType> dataTypeByKey,
        Dictionary<string, int> folderIdByPath)
    {
        existing.Alias = def.Alias;
        existing.Name = def.Name;
        existing.Icon = def.Icon ?? "icon-document";
        existing.Description = def.Description ?? string.Empty;
        existing.AllowedAsRoot = def.AllowedAtRoot;
        existing.IsElement = def.IsElement;

        // Move to correct folder if it has changed
        var targetParentId = def.Folder is not null && folderIdByPath.TryGetValue(def.Folder, out var fId) ? fId : -1;
        if (existing.ParentId != targetParentId)
            existing.ParentId = targetParentId;

        existing.PropertyGroups.Clear();
        ApplyProperties(existing, def, dataTypeByKey);
        await ApplyTemplateAsync(existing, def);

        var result = await _contentTypeService.UpdateAsync(existing, Constants.Security.SuperUserKey);
        if (result.Success)
            _logger.LogInformation("Updated content type '{Alias}' ({Key}).", def.Alias, def.Key);
        else
            _logger.LogError("Failed to update content type '{Alias}': {Status}.", def.Alias, result.Result);
    }

    private async Task ApplyTemplateAsync(IContentType contentType, DocumentTypeDefinition def)
    {
        if (def.DefaultTemplate is null)
        {
            contentType.AllowedTemplates = [];
            contentType.DefaultTemplateId = 0;
            return;
        }

        var template = await _templateService.GetAsync(def.DefaultTemplate);

        if (template is null)
        {
            var result = await _templateService.CreateAsync(
                def.DefaultTemplate,
                def.DefaultTemplate,
                content: null,
                Constants.Security.SuperUserKey);

            if (result.Success)
            {
                template = result.Result;
                _logger.LogInformation("Created template '{Alias}'.", def.DefaultTemplate);
            }
            else
            {
                _logger.LogError("Failed to create template '{Alias}': {Status}.", def.DefaultTemplate, result.Status);
                return;
            }
        }

        contentType.AllowedTemplates = [template];
        contentType.DefaultTemplateId = template.Id;
    }

    private void ApplyProperties(IContentType contentType, DocumentTypeDefinition def, Dictionary<Guid, IDataType> dataTypeByKey)
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
            contentType.PropertyGroups.Add(propertyGroup);

            foreach (var prop in group)
            {
                var descriptor = prop.DataType.GetDescriptor();
                if (!dataTypeByKey.TryGetValue(descriptor.Key, out var dataType))
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

                contentType.AddPropertyType(propertyType, groupAlias, group.Key);
            }
        }
    }

    private static void ApplyAllowedChildren(IContentType contentType, DocumentTypeDefinition def, Dictionary<Guid, string> aliasByKey)
    {
        var sorts = new List<ContentTypeSort>();

        for (var i = 0; i < def.AllowedChildTypes.Count; i++)
        {
            var childType = def.AllowedChildTypes[i];
            var childAttr = (DocumentTypeAttribute?)Attribute.GetCustomAttribute(childType, typeof(DocumentTypeAttribute));
            if (childAttr is null)
                continue;

            var alias = aliasByKey.GetValueOrDefault(childAttr.Key, childAttr.Alias ?? DocumentTypeScanner.ToAlias(childType.Name));
            sorts.Add(new ContentTypeSort(childAttr.Key, i, alias));
        }

        contentType.AllowedContentTypes = sorts;
    }
}
