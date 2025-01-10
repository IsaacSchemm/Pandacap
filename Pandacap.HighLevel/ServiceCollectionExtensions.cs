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
using Pandacap.LowLevel;

namespace Pandacap.HighLevel
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPandacapServices(
            this IServiceCollection services)
        {
            return services
                .AddScoped<ActivityPub.Mapper>()
                .AddScoped<ActivityPub.ProfileTranslator>()
                .AddScoped<ActivityPub.PostTranslator>()
                .AddScoped<ActivityPub.RelationshipTranslator>()
                .AddScoped<ActivityPub.InteractionTranslator>()
                .AddScoped<ActivityPubNotificationHandler>()
                .AddScoped<ActivityPubReplyNotificationHandler>()
                .AddScoped<ActivityPubRequestHandler>()
                .AddScoped<AtomRssFeedReader>()
                .AddScoped<ATProtoCredentialProvider>()
                .AddScoped<ATProtoDIDResolver>()
                .AddScoped<ATProtoNotificationHandler>()
                .AddScoped<BlueskyAgent>()
                .AddScoped<CompositeNotificationHandler>()
                .AddScoped<ComputerVisionProvider>()
                .AddScoped<DeviantArtCredentialProvider>()
                .AddScoped<DeviantArtFeedNotificationHandler>()
                .AddScoped<DeviantArtNoteNotificationHandler>()
                .AddScoped<FeedBuilder>()
                .AddScoped<FurAffinityNoteNotificationHandler>()
                .AddScoped<FurAffinityNotificationHandler>()
                .AddScoped<FurAffinityTimeZoneCache>()
                .AddScoped<ActivityPub.IHostInformationProvider, HostInformationProvider>()
                .AddScoped<JsonLdExpansionService>()
                .AddScoped<KeyProvider>()
                .AddScoped<LemmyClient>()
                .AddScoped<WeasylClientFactory>()
                .AddScoped<WeasylNoteNotificationHandler>()
                .AddScoped<WeasylNotificationHandler>();
        }
    }
}
