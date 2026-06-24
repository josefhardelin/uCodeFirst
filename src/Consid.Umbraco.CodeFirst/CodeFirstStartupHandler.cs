using Consid.Umbraco.CodeFirst.Sync;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Consid.Umbraco.CodeFirst;

internal sealed class CodeFirstStartupHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly CodeFirstSyncService _syncService;
    private readonly ILogger<CodeFirstStartupHandler> _logger;

    public CodeFirstStartupHandler(
        CodeFirstSyncService syncService,
        ILogger<CodeFirstStartupHandler> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
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
