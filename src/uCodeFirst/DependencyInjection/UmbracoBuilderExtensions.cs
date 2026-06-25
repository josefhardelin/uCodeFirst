using uCodeFirst.Discovery;
using uCodeFirst.Sync;
using uCodeFirst.Validation;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

namespace uCodeFirst.DependencyInjection;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddCodeFirst(this IUmbracoBuilder builder)
    {
        builder.Services
            .AddSingleton<DocumentTypeScanner>()
            .AddSingleton<PreFlightValidator>()
            .AddSingleton<DataTypeSyncEngine>()
            .AddSingleton<ContentTypeSyncEngine>()
            .AddSingleton<CodeFirstSyncService>();

        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, CodeFirstStartupHandler>();

        return builder;
    }
}
