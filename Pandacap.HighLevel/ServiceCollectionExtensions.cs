using Microsoft.Extensions.DependencyInjection;
using Pandacap.ActivityPub.Communication;
using Pandacap.HighLevel.ATProto;
using Pandacap.HighLevel.DeviantArt;
using Pandacap.HighLevel.FurAffinity;
using Pandacap.HighLevel.Lemmy;
using Pandacap.HighLevel.Notifications;
using Pandacap.HighLevel.RssInbound;
using Pandacap.HighLevel.RssOutbound;
using Pandacap.HighLevel.Weasyl;

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
                .AddScoped<ATProtoNotificationHandler>()
                .AddScoped<BlueskyAgent>()
                .AddScoped<CompositeNotificationHandler>()
                .AddScoped<DeviantArtCredentialProvider>()
                .AddScoped<DeviantArtFeedNotificationHandler>()
                .AddScoped<DeviantArtNoteNotificationHandler>()
                .AddScoped<FeedBuilder>()
                .AddScoped<FurAffinityNoteNotificationHandler>()
                .AddScoped<FurAffinityNotificationHandler>()
                .AddScoped<FurAffinityTimeZoneCache>()
                .AddScoped<JsonLdExpansionService>()
                .AddScoped<KeyProvider>()
                .AddScoped<LemmyClient>()
                .AddScoped<WeasylClientFactory>()
                .AddScoped<WeasylNoteNotificationHandler>()
                .AddScoped<WeasylNotificationHandler>();
        }
    }
}
