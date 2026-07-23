using uCodeFirst.Configuration;
using uCodeFirst.Sync;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace uCodeFirst;

internal sealed class CodeFirstStartupHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly CodeFirstSyncService _syncService;
    private readonly IRuntimeState _runtimeState;
    private readonly IOptions<CodeFirstOptions> _options;
    private readonly ILogger<CodeFirstStartupHandler> _logger;

    public CodeFirstStartupHandler(
        CodeFirstSyncService syncService,
        IRuntimeState runtimeState,
        IOptions<CodeFirstOptions> options,
        ILogger<CodeFirstStartupHandler> logger)
    {
        _syncService = syncService;
        _runtimeState = runtimeState;
        _options = options;
        _logger = logger;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        if (_runtimeState.Level != RuntimeLevel.Run)
        {
            _logger.LogInformation("Code-first sync skipped — runtime level is {Level}, database not yet installed.", _runtimeState.Level);
            return;
        }

        var options = _options.Value;

        try
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            if (options.Enabled)
                await _syncService.SyncAsync(assemblies, options.Strategy, cancellationToken);
            else
                await _syncService.PlanAsync(assemblies, options.Strategy, cancellationToken);
        }
        catch (Exception ex)
        {
            if (options.Enabled)
            {
                _logger.LogError(ex, "Code-first startup sync failed.");
                throw;
            }

            // uCodeFirst:Enabled=false is a permanent, safe dry-run preview — it must never block startup,
            // even on pre-flight validation failures on a legacy codebase being onboarded.
            _logger.LogWarning(ex, "Code-first dry-run preview failed — startup continues since uCodeFirst is not Enabled.");
        }
    }
}
