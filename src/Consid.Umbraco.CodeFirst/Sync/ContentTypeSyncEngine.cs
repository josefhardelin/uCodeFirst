using System.Reflection;
using Consid.Umbraco.CodeFirst.Attributes;
using Consid.Umbraco.CodeFirst.Discovery;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using UmbConstants = global::Umbraco.Cms.Core.Constants;

namespace Consid.Umbraco.CodeFirst.Sync;

internal sealed class ContentTypeSyncEngine
{
    private readonly IContentTypeService _contentTypeService;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly ILogger<ContentTypeSyncEngine> _logger;

    public ContentTypeSyncEngine(
        IContentTypeService contentTypeService,
        IShortStringHelper shortStringHelper,
        ILogger<ContentTypeSyncEngine> logger)
    {
        _contentTypeService = contentTypeService;
        _shortStringHelper = shortStringHelper;
        _logger = logger;
    }

    public async Task SyncAsync(
        IReadOnlyList<DocumentTypeDefinition> definitions,
        Dictionary<Guid, IDataType> dataTypeByKey,
        CancellationToken ct = default)
    {
        var aliasByKey = definitions.ToDictionary(d => d.Key, d => d.Alias);

        // Pass 1: create/update structure (without AllowedChildren to avoid ordering issues)
        foreach (var def in definitions)
        {
            var existing = await _contentTypeService.GetAsync(def.Key);
            if (existing is null)
                await CreateAsync(def, dataTypeByKey);
            else
                await UpdateAsync(existing, def, dataTypeByKey);
        }

        // Pass 2: wire up AllowedChildren now that all types exist
        foreach (var def in definitions)
        {
            if (def.AllowedChildTypes.Count == 0)
                continue;

            var contentType = await _contentTypeService.GetAsync(def.Key);
            if (contentType is null)
                continue;

            ApplyAllowedChildren(contentType, def, aliasByKey);
            var result = await _contentTypeService.UpdateAsync(contentType, UmbConstants.Security.SuperUserKey);
            if (!result.Success)
                _logger.LogError("Failed to set AllowedChildren on '{Alias}': {Status}.", def.Alias, result.Result);
        }
    }

    private async Task CreateAsync(DocumentTypeDefinition def, Dictionary<Guid, IDataType> dataTypeByKey)
    {
        var contentType = new ContentType(_shortStringHelper, parentId: -1)
        {
            Key = def.Key,
            Alias = def.Alias,
            Name = def.Name,
            Icon = def.Icon ?? "icon-document",
            Description = def.Description ?? string.Empty,
            AllowedAsRoot = def.AllowedAtRoot
        };

        ApplyProperties(contentType, def, dataTypeByKey);

        var result = await _contentTypeService.CreateAsync(contentType, UmbConstants.Security.SuperUserKey);
        if (result.Success)
            _logger.LogInformation("Created document type '{Alias}' ({Key}).", def.Alias, def.Key);
        else
            _logger.LogError("Failed to create document type '{Alias}': {Status}.", def.Alias, result.Result);
    }

    private async Task UpdateAsync(IContentType existing, DocumentTypeDefinition def, Dictionary<Guid, IDataType> dataTypeByKey)
    {
        existing.Alias = def.Alias;
        existing.Name = def.Name;
        existing.Icon = def.Icon ?? "icon-document";
        existing.Description = def.Description ?? string.Empty;
        existing.AllowedAsRoot = def.AllowedAtRoot;

        existing.PropertyGroups.Clear();
        ApplyProperties(existing, def, dataTypeByKey);

        var result = await _contentTypeService.UpdateAsync(existing, UmbConstants.Security.SuperUserKey);
        if (result.Success)
            _logger.LogInformation("Updated document type '{Alias}' ({Key}).", def.Alias, def.Key);
        else
            _logger.LogError("Failed to update document type '{Alias}': {Status}.", def.Alias, result.Result);
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
                var recipe = EditorRecipeResolver.Resolve(prop.EditorAttribute);
                if (!dataTypeByKey.TryGetValue(recipe.Key, out var dataType))
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
