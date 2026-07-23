using uCodeFirst.Discovery;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace uCodeFirst.Sync;

// Code owns dictionary item keys and hierarchy only — never translation values. Existing items
// (leaf or auto-created parent) are left completely untouched; only missing items are created.
// Additive-only, like the content/media type engines: nothing is ever deleted.
//
// NOTE: unlike DataTypeSyncEngine/LanguageSyncEngine/TemplateSyncEngine, this engine cannot grow
// update-on-drift logic without a prior, separate API decision: [DictionaryItem] only captures a
// key and hierarchy today (see DictionaryItemDefinition) — there is no way to declare translation
// text in C# at all yet, so there is nothing yet to diff an existing item's values against.
internal sealed class DictionaryItemSyncEngine
{
    private readonly IDictionaryItemService _dictionaryItemService;
    private readonly ILogger<DictionaryItemSyncEngine> _logger;

    public DictionaryItemSyncEngine(IDictionaryItemService dictionaryItemService, ILogger<DictionaryItemSyncEngine> logger)
    {
        _dictionaryItemService = dictionaryItemService;
        _logger = logger;
    }

    public async Task SyncAsync(IReadOnlyList<DictionaryItemDefinition> definitions, CancellationToken ct = default)
    {
        var resolved = new Dictionary<string, Guid?>(StringComparer.OrdinalIgnoreCase);

        foreach (var def in definitions)
        {
            Guid? parentKey = null;
            foreach (var container in def.ParentChain)
                parentKey = await EnsureItemAsync(container.Name, parentKey, resolved);

            await EnsureItemAsync(def.ItemKey, parentKey, resolved);
        }
    }

    private async Task<Guid?> EnsureItemAsync(string itemKey, Guid? parentKey, Dictionary<string, Guid?> resolved)
    {
        if (resolved.TryGetValue(itemKey, out var cached))
            return cached;

        var existing = await _dictionaryItemService.GetAsync(itemKey);
        if (existing is not null)
        {
            resolved[itemKey] = existing.Key;
            return existing.Key;
        }

        var item = new DictionaryItem(parentKey, itemKey);
        var result = await _dictionaryItemService.CreateAsync(item, Constants.Security.SuperUserKey);

        if (!result.Success)
        {
            _logger.LogError("Failed to create dictionary item '{ItemKey}': {Status}.", itemKey, result.Status);
            resolved[itemKey] = null;
            return null;
        }

        _logger.LogInformation("Created dictionary item '{ItemKey}'.", itemKey);
        resolved[itemKey] = result.Result.Key;
        return result.Result.Key;
    }
}
