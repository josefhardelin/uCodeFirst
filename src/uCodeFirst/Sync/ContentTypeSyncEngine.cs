using System.Security.Cryptography;
using System.Text;
using uCodeFirst.Attributes;
using uCodeFirst.Configuration;
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
        CodeFirstStrategy strategy,
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
                await UpdateAsync(existing, def, dataTypeByKey, folderIdByPath, strategy);
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

    // Read-only preview of what SyncAsync would do — no writes. AllowedChildren and compositions are
    // not part of the plan: they already have their own existing unconditional/scoped update behavior
    // and Strategy doesn't gate them.
    public async Task<TypeSyncPlan> PlanAsync(
        IReadOnlyList<DocumentTypeDefinition> definitions,
        CodeFirstStrategy strategy,
        CancellationToken ct = default)
    {
        var plan = new TypeSyncPlan();

        foreach (var def in definitions)
        {
            var existing = await _contentTypeService.GetAsync(def.Key);
            if (existing is null)
            {
                plan.ToCreate.Add(new PlanItem(def.Alias, def.Key));
                continue;
            }

            plan.ToUpdate.Add(new PlanItem(def.Alias, def.Key));

            if (strategy != CodeFirstStrategy.Destructive)
                continue;

            CollectStalePropertiesAndGroups(existing.PropertyTypes, existing.PropertyGroups, def.Properties, def.Alias, plan);
        }

        return plan;
    }

    private static void CollectStalePropertiesAndGroups(
        IEnumerable<IPropertyType> existingProperties,
        IEnumerable<PropertyGroup> existingGroups,
        IReadOnlyList<PropertyDefinition> currentProperties,
        string typeAlias,
        TypeSyncPlan plan)
    {
        var currentAliases = currentProperties.Select(p => p.Alias).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var staleProps = existingProperties.Where(pt => !currentAliases.Contains(pt.Alias)).ToList();

        var affectedGroupAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var stale in staleProps)
        {
            plan.PrunedProperties.Add(new PrunedProperty(typeAlias, stale.Alias));

            var group = existingGroups.FirstOrDefault(g => g.PropertyTypes.Any(pt => string.Equals(pt.Alias, stale.Alias, StringComparison.OrdinalIgnoreCase)));
            if (group is not null)
                affectedGroupAliases.Add(group.Alias);
        }

        foreach (var groupAlias in affectedGroupAliases)
        {
            var group = existingGroups.FirstOrDefault(g => string.Equals(g.Alias, groupAlias, StringComparison.OrdinalIgnoreCase));
            if (group is null)
                continue;

            var remaining = group.PropertyTypes.Count(pt => !staleProps.Any(s => string.Equals(s.Alias, pt.Alias, StringComparison.OrdinalIgnoreCase)));
            if (remaining == 0)
                plan.PrunedGroups.Add(new PrunedGroup(typeAlias, group.Alias));
        }
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
            Icon = BuildIconString(def.Icon, def.Color),
            Description = def.Description ?? string.Empty,
            AllowedAsRoot = def.AllowedAtRoot,
            IsElement = def.IsElement,
            Variations = def.VariesByCulture ? ContentVariation.Culture : ContentVariation.Nothing,
            ListView = def.IsContainer ? Constants.DataTypes.Guids.ListViewContentGuid : null
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
        Dictionary<string, int> folderIdByPath,
        CodeFirstStrategy strategy)
    {
        existing.Alias = def.Alias;
        existing.Name = def.Name;
        existing.Icon = BuildIconString(def.Icon, def.Color);
        existing.Description = def.Description ?? string.Empty;
        existing.AllowedAsRoot = def.AllowedAtRoot;
        existing.IsElement = def.IsElement;
        existing.Variations = def.VariesByCulture ? ContentVariation.Culture : ContentVariation.Nothing;
        existing.ListView = def.IsContainer ? Constants.DataTypes.Guids.ListViewContentGuid : null;

        // Move to correct folder if it has changed
        var targetParentId = def.Folder is not null && folderIdByPath.TryGetValue(def.Folder, out var fId) ? fId : -1;
        if (existing.ParentId != targetParentId)
            existing.ParentId = targetParentId;

        MergeProperties(existing, def, dataTypeByKey);

        if (strategy == CodeFirstStrategy.Destructive)
            PruneStaleProperties(existing, def);

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
                    SortOrder = prop.SortOrder,
                    Variations = prop.VariesByCulture ? ContentVariation.Culture : ContentVariation.Nothing
                };

                contentType.AddPropertyType(propertyType, groupAlias, group.Key);
            }
        }
    }

    private void MergeProperties(IContentType contentType, DocumentTypeDefinition def, Dictionary<Guid, IDataType> dataTypeByKey)
    {
        var groupedProps = def.Properties
            .GroupBy(p => p.GroupName)
            .Select((g, i) => (Group: g, Index: i))
            .ToList();

        foreach (var (group, groupIndex) in groupedProps)
        {
            var groupAlias = DocumentTypeScanner.ToAlias(group.Key);

            // Find or create the property group — never wipe it
            var propertyGroup = contentType.PropertyGroups
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
                contentType.PropertyGroups.Add(propertyGroup);
            }
            else
            {
                propertyGroup.Name = group.Key;
                propertyGroup.SortOrder = groupIndex;
            }

            foreach (var prop in group)
            {
                var descriptor = prop.DataType.GetDescriptor();
                if (!dataTypeByKey.TryGetValue(descriptor.Key, out var dataType))
                {
                    _logger.LogWarning("Data type for property '{Alias}' on '{Type}' not found — skipping.", prop.Alias, def.ClrType.Name);
                    continue;
                }

                // Update existing property type in-place to preserve its ID and content data
                var existingProp = contentType.PropertyTypes
                    .FirstOrDefault(pt => string.Equals(pt.Alias, prop.Alias, StringComparison.OrdinalIgnoreCase));

                if (existingProp is not null)
                {
                    existingProp.Name = prop.Name;
                    existingProp.Mandatory = prop.Mandatory;
                    existingProp.Description = prop.Description ?? string.Empty;
                    existingProp.SortOrder = prop.SortOrder;
                    existingProp.DataTypeKey = dataType.Key;
                    existingProp.Variations = prop.VariesByCulture ? ContentVariation.Culture : ContentVariation.Nothing;
                }
                else
                {
                    var propertyType = new PropertyType(_shortStringHelper, dataType, prop.Alias)
                    {
                        Name = prop.Name,
                        Mandatory = prop.Mandatory,
                        Description = prop.Description ?? string.Empty,
                        SortOrder = prop.SortOrder,
                        Variations = prop.VariesByCulture ? ContentVariation.Culture : ContentVariation.Nothing
                    };
                    contentType.AddPropertyType(propertyType, groupAlias, group.Key);
                }
            }
        }
    }

    // Strategy.Destructive only. Removes PropertyTypes whose alias is no longer in the current C#
    // definition, then removes any PropertyGroup left empty as a result. No provenance tracking —
    // anything hand-added in the backoffice on a code-first-managed type is removed too if its alias
    // isn't in the C# definition.
    private void PruneStaleProperties(IContentType contentType, DocumentTypeDefinition def)
    {
        var currentAliases = def.Properties.Select(p => p.Alias).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var staleProps = contentType.PropertyTypes.Where(pt => !currentAliases.Contains(pt.Alias)).ToList();
        if (staleProps.Count == 0)
            return;

        var affectedGroupAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var stale in staleProps)
        {
            var group = contentType.PropertyGroups.FirstOrDefault(g => g.PropertyTypes.Any(pt => string.Equals(pt.Alias, stale.Alias, StringComparison.OrdinalIgnoreCase)));
            if (group is not null)
                affectedGroupAliases.Add(group.Alias);

            contentType.RemovePropertyType(stale.Alias);
            _logger.LogInformation("Pruned stale property '{Alias}' from '{Type}' (Destructive).", stale.Alias, def.Alias);
        }

        foreach (var groupAlias in affectedGroupAliases)
        {
            var group = contentType.PropertyGroups.FirstOrDefault(g => string.Equals(g.Alias, groupAlias, StringComparison.OrdinalIgnoreCase));
            if (group is not null && group.PropertyTypes.Count == 0)
            {
                contentType.PropertyGroups.Remove(groupAlias);
                _logger.LogInformation("Pruned empty property group '{Group}' from '{Type}' (Destructive).", groupAlias, def.Alias);
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

    private static string BuildIconString(string? icon, string? color)
    {
        var base_ = icon ?? "icon-document";
        return color is not null ? $"{base_} {color}" : base_;
    }
}
