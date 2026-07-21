using System.Reflection;
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

    public async Task SyncAsync(IEnumerable<Assembly> assemblies, CancellationToken ct = default)
    {
        var assemblyList = assemblies.ToList();

        var definitions = _scanner.Scan(assemblyList);
        var mediaDefinitions = _scanner.ScanMediaTypes(assemblyList);
        var dictionaryDefinitions = _scanner.ScanDictionaryItems(assemblyList);
        var languageSetDefinitions = _scanner.ScanLanguages(assemblyList);
        var templateDefinitions = _scanner.ScanTemplates(assemblyList);

        if (definitions.Count == 0 && mediaDefinitions.Count == 0 && dictionaryDefinitions.Count == 0 && languageSetDefinitions.Count == 0 && templateDefinitions.Count == 0)
        {
            _logger.LogDebug("Code-first: no [DocumentType], [MediaType], [DictionaryItem], [Languages], or [Template] members found.");
            return;
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

        if (templateDefinitions.Count > 0)
            await _templateSyncEngine.SyncAsync(templateDefinitions, ct);

        if (definitions.Count > 0)
        {
            var dataTypeByKey = await _dataTypeSyncEngine.EnsureDataTypesAsync(definitions, ct);
            await _contentTypeSyncEngine.SyncAsync(definitions, dataTypeByKey, ct);
        }

        if (mediaDefinitions.Count > 0)
        {
            var mediaDataTypeByKey = await _dataTypeSyncEngine.EnsureMediaDataTypesAsync(mediaDefinitions, ct);
            await _mediaTypeSyncEngine.SyncAsync(mediaDefinitions, mediaDataTypeByKey, ct);
        }

        if (dictionaryDefinitions.Count > 0)
            await _dictionaryItemSyncEngine.SyncAsync(dictionaryDefinitions, ct);

        if (languageSetDefinitions.Count == 1)
            await _languageSyncEngine.SyncAsync(languageSetDefinitions[0], ct);

        _logger.LogInformation("Code-first sync complete.");
    }
}
