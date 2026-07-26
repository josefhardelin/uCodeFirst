using System.Reflection;
using uCodeFirst.Attributes;
using uCodeFirst.Discovery;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

namespace uCodeFirst.Sync;

// Create-only, like DictionaryItemSyncEngine/LanguageSyncEngine — an existing node (matched by its
// declared Key via IContentService.GetById(Guid)) is never updated or deleted, only created when
// absent. A seed is deliberately an *empty* stub: no property values are ever written (that's a
// separate, still-open roadmap item requiring a source generator + its own pre-flight validation).
// The stub is saved and immediately published so it's usable right away by anything that references
// its GUID (e.g. a future MultiNodeTreePicker dynamic-root ByKey origin).
//
// A seed may declare a Parent that is itself another [SeedContent] type, so parents must be resolved
// (and created) before their children. PreFlightValidator has already ruled out dangling Parent refs
// and Parent cycles by the time this engine runs, so resolution here just walks the chain recursively
// — mirroring LanguageSyncEngine's recursive Fallback resolution — with a defensive `visited` guard
// against runaway recursion rather than re-deriving that guarantee.
internal sealed class ContentSeedingEngine
{
    private readonly IContentService _contentService;
    private readonly ILogger<ContentSeedingEngine> _logger;

    public ContentSeedingEngine(IContentService contentService, ILogger<ContentSeedingEngine> logger)
    {
        _contentService = contentService;
        _logger = logger;
    }

    public Task SyncAsync(IReadOnlyList<SeedContentDefinition> definitions, CancellationToken ct = default)
    {
        var byType = definitions.ToDictionary(d => d.ClrType);
        var resolvedIds = new Dictionary<Type, int>();
        var visiting = new HashSet<Type>();

        foreach (var def in definitions)
            EnsureSeed(def, byType, resolvedIds, visiting);

        return Task.CompletedTask;
    }

    // Returns the created/existing node's int id (needed as the parentId for Create), or null if the
    // seed couldn't be resolved/created (already logged).
    private int? EnsureSeed(
        SeedContentDefinition def,
        IReadOnlyDictionary<Type, SeedContentDefinition> byType,
        Dictionary<Type, int> resolvedIds,
        HashSet<Type> visiting)
    {
        if (resolvedIds.TryGetValue(def.ClrType, out var cachedId))
            return cachedId;

        if (!visiting.Add(def.ClrType))
            return null; // defensive only — PreFlightValidator already rejects real Parent cycles

        int? parentId = null;
        if (def.Parent is not null && byType.TryGetValue(def.Parent, out var parentDef))
            parentId = EnsureSeed(parentDef, byType, resolvedIds, visiting);

        var existing = _contentService.GetById(def.Key);
        if (existing is not null)
        {
            resolvedIds[def.ClrType] = existing.Id;
            return existing.Id;
        }

        var docTypeAttr = def.DocumentType.GetCustomAttribute<DocumentTypeAttribute>();
        if (docTypeAttr is null)
        {
            // Dangling reference — PreFlightValidator already reports this as an error; nothing to do.
            return null;
        }

        var alias = docTypeAttr.Alias ?? DocumentTypeScanner.ToAlias(def.DocumentType.Name);
        var content = _contentService.Create(def.Name, parentId ?? Constants.System.Root, alias);
        content.Key = def.Key;

        var saveResult = _contentService.Save(content);
        if (!saveResult.Success)
        {
            _logger.LogError("Failed to save seed content '{Name}' ({Key}): {Status}.", def.Name, def.Key, saveResult.Result);
            return null;
        }

        var publishResult = _contentService.Publish(content, new[] { "*" });
        if (!publishResult.Success)
            _logger.LogError("Failed to publish seed content '{Name}' ({Key}): {Status}.", def.Name, def.Key, publishResult.Result);

        _logger.LogInformation("Created seed content '{Name}' ({Key}).", def.Name, def.Key);

        resolvedIds[def.ClrType] = content.Id;
        return content.Id;
    }
}
