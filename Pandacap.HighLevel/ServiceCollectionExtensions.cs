using Microsoft.Extensions.DependencyInjection;
using Pandacap.HighLevel.Notifications;
using Pandacap.LowLevel;

namespace Pandacap.HighLevel
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPandacapServices(
            this IServiceCollection services,
            ApplicationInformation applicationInformation)
        {
            return services
                .AddSingleton(applicationInformation)
                .AddScoped<ActivityPubNotificationHandler>()
                .AddScoped<ActivityPubRequestHandler>()
                .AddScoped<ActivityPubReplyHandler>()
                .AddScoped<ActivityPubTranslator>()
                .AddScoped<AtomRssFeedReader>()
                .AddScoped<ATProtoCredentialProvider>()
                .AddScoped<ATProtoInboxHandler>()
                .AddScoped<ATProtoNotificationHandler>()
                .AddScoped<BlueskyAgent>()
                .AddScoped<DeviantArtCredentialProvider>()
                .AddScoped<DeviantArtInboxHandler>()
                .AddScoped<DeviantArtNotificationHandler>()
                .AddScoped<IdMapper>()
                .AddScoped<KeyProvider>()
                .AddScoped<OutboxProcessor>()
                .AddScoped<WeasylClientFactory>()
                .AddScoped<WeasylInboxHandler>()
                .AddScoped<WeasylNotificationHandler>();
        }
    }
}
