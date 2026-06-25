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
    private readonly ILogger<CodeFirstSyncService> _logger;

    public CodeFirstSyncService(
        DocumentTypeScanner scanner,
        PreFlightValidator validator,
        DataTypeSyncEngine dataTypeSyncEngine,
        ContentTypeSyncEngine contentTypeSyncEngine,
        ILogger<CodeFirstSyncService> logger)
    {
        _scanner = scanner;
        _validator = validator;
        _dataTypeSyncEngine = dataTypeSyncEngine;
        _contentTypeSyncEngine = contentTypeSyncEngine;
        _logger = logger;
    }

    public async Task SyncAsync(IEnumerable<Assembly> assemblies, CancellationToken ct = default)
    {
        var definitions = _scanner.Scan(assemblies);

        if (definitions.Count == 0)
        {
            _logger.LogDebug("Code-first: no [DocumentType] classes found.");
            return;
        }

        _logger.LogInformation("Code-first: discovered {Count} document type(s).", definitions.Count);

        var errors = _validator.Validate(definitions);
        if (errors.Count > 0)
        {
            var bullet = string.Join("\n  - ", errors);
            throw new InvalidOperationException(
                $"Code-first pre-flight validation failed with {errors.Count} error(s):\n  - {bullet}");
        }

        var dataTypeByKey = await _dataTypeSyncEngine.EnsureDataTypesAsync(definitions, ct);
        await _contentTypeSyncEngine.SyncAsync(definitions, dataTypeByKey, ct);

        _logger.LogInformation("Code-first sync complete.");
    }
}
