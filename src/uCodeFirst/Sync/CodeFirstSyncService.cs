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
    private readonly ILogger<CodeFirstSyncService> _logger;

    public CodeFirstSyncService(
        DocumentTypeScanner scanner,
        PreFlightValidator validator,
        DataTypeSyncEngine dataTypeSyncEngine,
        ContentTypeSyncEngine contentTypeSyncEngine,
        MediaTypeSyncEngine mediaTypeSyncEngine,
        ILogger<CodeFirstSyncService> logger)
    {
        _scanner = scanner;
        _validator = validator;
        _dataTypeSyncEngine = dataTypeSyncEngine;
        _contentTypeSyncEngine = contentTypeSyncEngine;
        _mediaTypeSyncEngine = mediaTypeSyncEngine;
        _logger = logger;
    }

    public async Task SyncAsync(IEnumerable<Assembly> assemblies, CancellationToken ct = default)
    {
        var assemblyList = assemblies.ToList();

        var definitions = _scanner.Scan(assemblyList);
        var mediaDefinitions = _scanner.ScanMediaTypes(assemblyList);

        if (definitions.Count == 0 && mediaDefinitions.Count == 0)
        {
            _logger.LogDebug("Code-first: no [DocumentType] or [MediaType] classes found.");
            return;
        }

        _logger.LogInformation("Code-first: discovered {DocCount} document type(s), {MediaCount} media type(s).",
            definitions.Count, mediaDefinitions.Count);

        var errors = _validator.Validate(definitions, mediaDefinitions);
        if (errors.Count > 0)
        {
            var bullet = string.Join("\n  - ", errors);
            throw new InvalidOperationException(
                $"Code-first pre-flight validation failed with {errors.Count} error(s):\n  - {bullet}");
        }

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

        _logger.LogInformation("Code-first sync complete.");
    }
}
