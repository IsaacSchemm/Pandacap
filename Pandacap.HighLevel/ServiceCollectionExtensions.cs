using Microsoft.Extensions.DependencyInjection;
using Pandacap.ActivityPub.Communication;
using Pandacap.ConfigurationObjects;
using Pandacap.HighLevel.ATProto;
using Pandacap.HighLevel.DeviantArt;
using Pandacap.HighLevel.Lemmy;
using Pandacap.HighLevel.Notifications;
using Pandacap.HighLevel.RssInbound;
using Pandacap.HighLevel.RssOutbound;
using Pandacap.HighLevel.Weasyl;
using Pandacap.Clients;
using Pandacap.LowLevel.MyLinks;

namespace Pandacap.HighLevel
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPandacapServices(
            this IServiceCollection services,
            ApplicationInformation appInfo)
        {
            return services
                .AddSingleton(appInfo)
                .AddSingleton(new ActivityPub.HostInformation(
                    applicationHostname: appInfo.ApplicationHostname,
                    applicationName: UserAgentInformation.ApplicationName,
                    websiteUrl: UserAgentInformation.WebsiteUrl))
                .AddScoped<ActivityPub.Mapper>()
                .AddScoped<ActivityPub.ProfileTranslator>()
                .AddScoped<ActivityPub.PostTranslator>()
                .AddScoped<ActivityPub.RelationshipTranslator>()
                .AddScoped<ActivityPub.InteractionTranslator>()
                .AddScoped<IActivityPubCommunicationPrerequisites, ActivityPubCommunicationPrerequisites>()
                .AddScoped<ActivityPubNotificationHandler>()
                .AddScoped<ActivityPubReplyNotificationHandler>()
                .AddScoped<ActivityPubRequestHandler>()
                .AddScoped<AtomRssFeedReader>()
                .AddScoped<ATProtoCredentialProvider>()
                .AddScoped<ATProtoDIDResolver>()
                .AddScoped<ATProtoNotificationHandler>()
                .AddScoped<BlueskyAgent>()
                .AddScoped<BlueskyProfileResolver>()
                .AddScoped<CompositeFavoritesProvider>()
                .AddScoped<CompositeNotificationHandler>()
                .AddScoped<ComputerVisionProvider>()
                .AddScoped<DeviantArtCredentialProvider>()
                .AddScoped<DeviantArtFeedNotificationHandler>()
                .AddScoped<DeviantArtNoteNotificationHandler>()
                .AddScoped<FavoritesFeedBuilder>()
                .AddScoped<FeedBuilder>()
                .AddScoped<FurAffinityNoteNotificationHandler>()
                .AddScoped<FurAffinityNotificationHandler>()
                .AddScoped<FurryNetworkClient>()
                .AddScoped<JsonLdExpansionService>()
                .AddScoped<LemmyClient>()
                .AddScoped<IMyLinkService, MyLinkService>()
                .AddScoped<StarpassAgent>()
                .AddScoped<TwtxtClient>()
                .AddScoped<TwtxtFeedReader>()
                .AddScoped<WeasylClientFactory>()
                .AddScoped<WeasylNoteNotificationHandler>()
                .AddScoped<WeasylNotificationHandler>();
        }
    }
}
