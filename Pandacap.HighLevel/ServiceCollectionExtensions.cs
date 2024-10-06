using Microsoft.Extensions.DependencyInjection;
using Pandacap.HighLevel.Notifications;
using Pandacap.LowLevel;

namespace Pandacap.HighLevel
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSharedServices(
            this IServiceCollection services,
            ApplicationInformation applicationInformation)
        {
            return services
                .AddSingleton(applicationInformation)
                .AddScoped<ActivityPubNotificationHandler>()
                .AddScoped<ActivityPubReplyNotificationHandler>()
                .AddScoped<ActivityPubRequestHandler>()
                .AddScoped<ActivityPubTranslator>()
                .AddScoped<AtomRssFeedReader>()
                .AddScoped<ATProtoCredentialProvider>()
                .AddScoped<ATProtoDIDResolver>()
                .AddScoped<ATProtoInboxHandler>()
                .AddScoped<ATProtoLikesProvider>()
                .AddScoped<ATProtoNotificationHandler>()
                .AddScoped<BlueskyAgent>()
                .AddScoped<CompositeNotificationHandler>()
                .AddScoped<DeviantArtCredentialProvider>()
                .AddScoped<DeviantArtInboxHandler>()
                .AddScoped<DeviantArtFeedNotificationHandler>()
                .AddScoped<DeviantArtNoteNotificationHandler>()
                .AddScoped<FeedBuilder>()
                .AddScoped<IdMapper>()
                .AddScoped<JsonLdExpansionService>()
                .AddScoped<KeyProvider>()
                .AddScoped<OutboxProcessor>()
                .AddScoped<WeasylClientFactory>()
                .AddScoped<WeasylInboxHandler>()
                .AddScoped<WeasylNotificationHandler>();
        }
    }
}
