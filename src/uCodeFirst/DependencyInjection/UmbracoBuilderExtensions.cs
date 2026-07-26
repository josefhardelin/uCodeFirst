using uCodeFirst.Configuration;
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
            .AddOptions<CodeFirstOptions>()
            .Bind(builder.Config.GetSection("uCodeFirst"));

        builder.Services
            .AddSingleton<DocumentTypeScanner>()
            .AddSingleton<PreFlightValidator>()
            .AddSingleton<DataTypeSyncEngine>()
            .AddSingleton<ContentTypeSyncEngine>()
            .AddSingleton<MediaTypeSyncEngine>()
            .AddSingleton<DictionaryItemSyncEngine>()
            .AddSingleton<LanguageSyncEngine>()
            .AddSingleton<TemplateSyncEngine>()
            .AddSingleton<ContentSeedingEngine>()
            .AddSingleton<CodeFirstSyncService>();

        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, CodeFirstStartupHandler>();

        return builder;
    }
}
