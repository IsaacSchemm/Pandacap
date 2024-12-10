using Microsoft.Extensions.DependencyInjection;
using Pandacap.HighLevel.Notifications;

namespace Pandacap.HighLevel
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHighLevelServices(
            this IServiceCollection services)
        {
            return services
                .AddScoped<ActivityPubNotificationHandler>()
                .AddScoped<ActivityPubReplyNotificationHandler>()
                .AddScoped<ActivityPubRequestHandler>()
                .AddScoped<AtomRssFeedReader>()
                .AddScoped<ATProtoCredentialProvider>()
                .AddScoped<ATProtoDIDResolver>()
                .AddScoped<ATProtoInboxHandler>()
                .AddScoped<ATProtoNotificationHandler>()
                .AddScoped<BlueskyAgent>()
                .AddScoped<CompositeNotificationHandler>()
                .AddScoped<DeviantArtCredentialProvider>()
                .AddScoped<DeviantArtInboxHandler>()
                .AddScoped<DeviantArtFeedNotificationHandler>()
                .AddScoped<DeviantArtNoteNotificationHandler>()
                .AddScoped<FeedBuilder>()
                .AddScoped<FurAffinityInboxHandler>()
                .AddScoped<FurAffinityNoteNotificationHandler>()
                .AddScoped<FurAffinityNotificationHandler>()
                .AddScoped<FurAffinityTimeZoneCache>()
                .AddScoped<JsonLdExpansionService>()
                .AddScoped<KeyProvider>()
                .AddScoped<LemmyClient>()
                .AddScoped<OutboxProcessor>()
                .AddScoped<WeasylClientFactory>()
                .AddScoped<WeasylInboxHandler>()
                .AddScoped<WeasylNoteNotificationHandler>()
                .AddScoped<WeasylNotificationHandler>();
        }
    }
}
