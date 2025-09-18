using DnsClient;
using Microsoft.Extensions.DependencyInjection;
using Pandacap.ActivityPub.Communication;
using Pandacap.ActivityPub.Inbound;
using Pandacap.Clients;
using Pandacap.Clients.ATProto;
using Pandacap.ConfigurationObjects;
using Pandacap.HighLevel.ATProto;
using Pandacap.HighLevel.DeviantArt;
using Pandacap.HighLevel.Lemmy;
using Pandacap.HighLevel.RssInbound;
using Pandacap.HighLevel.RssOutbound;
using Pandacap.HighLevel.Weasyl;
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
                .AddSingleton<ILookupClient>(
                    new LookupClient(
                        new LookupClientOptions
                        {
                            UseCache = true
                        }))
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
                .AddScoped<ActivityPubRequestHandler>()
                .AddScoped<AtomRssFeedReader>()
                .AddScoped<ATProtoBackLinkIngestService>()
                .AddScoped<ATProtoFeedReader>()
                .AddScoped<ATProtoHandleLookupClient>()
                .AddScoped<BridgyFedDIDProvider>()
                .AddScoped<CompositeFavoritesProvider>()
                .AddScoped<ConstellationClient>()
                .AddScoped<DeviantArtCredentialProvider>()
                .AddScoped<DIDResolver>()
                .AddScoped<FavoritesFeedBuilder>()
                .AddScoped<FeedBuilder>()
                .AddScoped<FurryNetworkClient>()
                .AddScoped<JsonLdExpansionService>()
                .AddScoped<LemmyClient>()
                .AddScoped<IMyLinkService, MyLinkService>()
                .AddScoped<WeasylClientFactory>()
                .AddScoped<WebFingerService>();
        }
    }
}
