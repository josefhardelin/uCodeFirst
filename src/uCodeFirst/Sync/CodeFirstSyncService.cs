using System.Reflection;
using uCodeFirst.Configuration;
using uCodeFirst.Discovery;
using uCodeFirst.Validation;
using Microsoft.Extensions.Logging;

namespace uCodeFirst.Sync;

internal sealed class CodeFirstSyncService
{
    private readonly DocumentTypeScanner _scanner;
    private readonly PreFlightValidator _validator;
    private readonly DataTypeSyncEngine _dataTypeSyncEngine;
    private readonly ContentTypeSyncEngine _contentTypeSyncEngine;
    private readonly MediaTypeSyncEngine _mediaTypeSyncEngine;
    private readonly DictionaryItemSyncEngine _dictionaryItemSyncEngine;
    private readonly LanguageSyncEngine _languageSyncEngine;
    private readonly TemplateSyncEngine _templateSyncEngine;
    private readonly ILogger<CodeFirstSyncService> _logger;

    public CodeFirstSyncService(
        DocumentTypeScanner scanner,
        PreFlightValidator validator,
        DataTypeSyncEngine dataTypeSyncEngine,
        ContentTypeSyncEngine contentTypeSyncEngine,
        MediaTypeSyncEngine mediaTypeSyncEngine,
        DictionaryItemSyncEngine dictionaryItemSyncEngine,
        LanguageSyncEngine languageSyncEngine,
        TemplateSyncEngine templateSyncEngine,
        ILogger<CodeFirstSyncService> logger)
    {
        _scanner = scanner;
        _validator = validator;
        _dataTypeSyncEngine = dataTypeSyncEngine;
        _contentTypeSyncEngine = contentTypeSyncEngine;
        _mediaTypeSyncEngine = mediaTypeSyncEngine;
        _dictionaryItemSyncEngine = dictionaryItemSyncEngine;
        _languageSyncEngine = languageSyncEngine;
        _templateSyncEngine = templateSyncEngine;
        _logger = logger;
    }

    public async Task SyncAsync(IEnumerable<Assembly> assemblies, CodeFirstStrategy strategy, CancellationToken ct = default)
    {
        var scan = ScanAndValidate(assemblies.ToList());
        if (scan.IsEmpty)
            return;

        if (scan.TemplateDefinitions.Count > 0)
            await _templateSyncEngine.SyncAsync(scan.TemplateDefinitions, ct);

        if (scan.Definitions.Count > 0)
        {
            var dataTypeByKey = await _dataTypeSyncEngine.EnsureDataTypesAsync(scan.Definitions, ct);
            await _contentTypeSyncEngine.SyncAsync(scan.Definitions, dataTypeByKey, strategy, ct);
        }

        if (scan.MediaDefinitions.Count > 0)
        {
            var mediaDataTypeByKey = await _dataTypeSyncEngine.EnsureMediaDataTypesAsync(scan.MediaDefinitions, ct);
            await _mediaTypeSyncEngine.SyncAsync(scan.MediaDefinitions, mediaDataTypeByKey, strategy, ct);
        }

        if (scan.DictionaryDefinitions.Count > 0)
            await _dictionaryItemSyncEngine.SyncAsync(scan.DictionaryDefinitions, ct);

        if (scan.LanguageSetDefinitions.Count == 1)
            await _languageSyncEngine.SyncAsync(scan.LanguageSetDefinitions[0], ct);

        _logger.LogInformation("Code-first sync complete.");
    }

    // uCodeFirst:Enabled=false — computes the same plan as SyncAsync but never writes. Scoped to content
    // types and media types only (see TypeSyncPlan); data types, dictionary items, languages, and
    // templates aren't previewed yet since those engines have no plan/apply split.
    public async Task PlanAsync(IEnumerable<Assembly> assemblies, CodeFirstStrategy strategy, CancellationToken ct = default)
    {
        var scan = ScanAndValidate(assemblies.ToList());
        if (scan.IsEmpty)
            return;

        var result = await BuildPlanResultAsync(scan, strategy, enabled: false, ct);
        LogPlan(result, strategy);
    }

    // On-demand path for the backoffice dry-run dashboard (Api/PlanCodeFirstController) — a live,
    // fresh computation every call, no caching. `enabled` is passed through from the caller's
    // IOptions<CodeFirstOptions> snapshot rather than read here, since CodeFirstSyncService itself only
    // takes a strategy, not the full options object (see CodeFirstOptions). Works regardless of
    // Enabled — the caller decides what "enabled" means for the response (current config state), it
    // does not gate whether a plan can be computed.
    public async Task<CodeFirstPlanResult> ComputePlanAsync(
        IEnumerable<Assembly> assemblies,
        CodeFirstStrategy strategy,
        bool enabled,
        CancellationToken ct = default)
    {
        var scan = ScanAndValidate(assemblies.ToList());
        return await BuildPlanResultAsync(scan, strategy, enabled, ct);
    }

    private async Task<CodeFirstPlanResult> BuildPlanResultAsync(ScanResult scan, CodeFirstStrategy strategy, bool enabled, CancellationToken ct)
    {
        var contentPlan = scan.Definitions.Count > 0
            ? await _contentTypeSyncEngine.PlanAsync(scan.Definitions, strategy, ct)
            : new TypeSyncPlan();

        var mediaPlan = scan.MediaDefinitions.Count > 0
            ? await _mediaTypeSyncEngine.PlanAsync(scan.MediaDefinitions, strategy, ct)
            : new TypeSyncPlan();

        return new CodeFirstPlanResult
        {
            Enabled = enabled,
            Strategy = strategy.ToString(),
            GeneratedAtUtc = DateTime.UtcNow,
            ToCreate = contentPlan.ToCreate.Concat(mediaPlan.ToCreate).Select(i => i.Alias).ToList(),
            ToUpdate = contentPlan.ToUpdate.Concat(mediaPlan.ToUpdate).Select(i => i.Alias).ToList(),
            PrunedProperties = contentPlan.PrunedProperties.Concat(mediaPlan.PrunedProperties).ToList(),
            PrunedGroups = contentPlan.PrunedGroups.Concat(mediaPlan.PrunedGroups).ToList(),
        };
    }

    private void LogPlan(CodeFirstPlanResult result, CodeFirstStrategy strategy)
    {
        var prunedProperties = result.PrunedProperties.Select(p => $"{p.TypeAlias}.{p.PropertyAlias}").ToList();
        var prunedGroups = result.PrunedGroups.Select(g => $"{g.TypeAlias}.{g.GroupAlias}").ToList();

        var pruneSummary = strategy == CodeFirstStrategy.Destructive
            ? $"Would prune: {prunedProperties.Count} propert{(prunedProperties.Count == 1 ? "y" : "ies")} [{string.Join(", ", prunedProperties)}], {prunedGroups.Count} empty group(s) [{string.Join(", ", prunedGroups)}]."
            : "Would prune: N/A (Strategy=NonDestructive).";

        _logger.LogInformation(
            "uCodeFirst dry run (Enabled=false) — Would create: {CreateCount} [{Creates}]. Would update: {UpdateCount} [{Updates}]. {PruneSummary} " +
            "Dry-run preview does not yet cover data types, dictionary items, languages, or templates — only content types and media types.",
            result.ToCreate.Count, string.Join(", ", result.ToCreate),
            result.ToUpdate.Count, string.Join(", ", result.ToUpdate),
            pruneSummary);
    }

    private ScanResult ScanAndValidate(List<Assembly> assemblyList)
    {
        var definitions = _scanner.Scan(assemblyList);
        var mediaDefinitions = _scanner.ScanMediaTypes(assemblyList);
        var dictionaryDefinitions = _scanner.ScanDictionaryItems(assemblyList);
        var languageSetDefinitions = _scanner.ScanLanguages(assemblyList);
        var templateDefinitions = _scanner.ScanTemplates(assemblyList);

        if (definitions.Count == 0 && mediaDefinitions.Count == 0 && dictionaryDefinitions.Count == 0 && languageSetDefinitions.Count == 0 && templateDefinitions.Count == 0)
        {
            _logger.LogDebug("Code-first: no [DocumentType], [MediaType], [DictionaryItem], [Languages], or [Template] members found.");
            return new ScanResult(definitions, mediaDefinitions, dictionaryDefinitions, languageSetDefinitions, templateDefinitions, IsEmpty: true);
        }

        _logger.LogInformation(
            "Code-first: discovered {DocCount} document type(s), {MediaCount} media type(s), {DictCount} dictionary item(s), {LangCount} language(s), {TemplateCount} template(s).",
            definitions.Count, mediaDefinitions.Count, dictionaryDefinitions.Count, languageSetDefinitions.Sum(l => l.Languages.Count), templateDefinitions.Count);

        var errors = _validator.Validate(definitions, mediaDefinitions, dictionaryDefinitions, languageSetDefinitions, templateDefinitions);
        if (errors.Count > 0)
        {
            var bullet = string.Join("\n  - ", errors);
            throw new InvalidOperationException(
                $"Code-first pre-flight validation failed with {errors.Count} error(s):\n  - {bullet}");
        }

        return new ScanResult(definitions, mediaDefinitions, dictionaryDefinitions, languageSetDefinitions, templateDefinitions, IsEmpty: false);
    }

    private sealed record ScanResult(
        IReadOnlyList<DocumentTypeDefinition> Definitions,
        IReadOnlyList<MediaTypeDefinition> MediaDefinitions,
        IReadOnlyList<DictionaryItemDefinition> DictionaryDefinitions,
        IReadOnlyList<LanguageSetDefinition> LanguageSetDefinitions,
        IReadOnlyList<TemplateDefinition> TemplateDefinitions,
        bool IsEmpty);
}
