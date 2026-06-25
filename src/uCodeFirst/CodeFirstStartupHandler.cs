using uCodeFirst.Sync;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace uCodeFirst;

internal sealed class CodeFirstStartupHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly CodeFirstSyncService _syncService;
    private readonly IRuntimeState _runtimeState;
    private readonly ILogger<CodeFirstStartupHandler> _logger;

    public CodeFirstStartupHandler(
        CodeFirstSyncService syncService,
        IRuntimeState runtimeState,
        ILogger<CodeFirstStartupHandler> logger)
    {
        _syncService = syncService;
        _runtimeState = runtimeState;
        _logger = logger;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        if (_runtimeState.Level != RuntimeLevel.Run)
        {
            _logger.LogInformation("Code-first sync skipped — runtime level is {Level}, database not yet installed.", _runtimeState.Level);
            return;
        }

        try
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            await _syncService.SyncAsync(assemblies, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Code-first startup sync failed.");
            throw;
        }
    }
}
