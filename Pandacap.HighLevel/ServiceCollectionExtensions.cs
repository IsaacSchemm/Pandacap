using DnsClient;
using Microsoft.Extensions.DependencyInjection;
using Pandacap.ActivityPub;
using Pandacap.ActivityPub.Communication;
using Pandacap.ActivityPub.Inbound;
using Pandacap.Clients;
using Pandacap.Clients.ATProto;
using Pandacap.ConfigurationObjects;
using Pandacap.HighLevel.ATProto;
using Pandacap.HighLevel.DeviantArt;
using Pandacap.HighLevel.FeedReaders;
using Pandacap.HighLevel.Lemmy;
using Pandacap.HighLevel.PlatformLinks;
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
                .AddSingleton(new ActivityPubHostInformation(
                    applicationHostname: appInfo.ApplicationHostname,
                    applicationName: UserAgentInformation.ApplicationName,
                    websiteUrl: UserAgentInformation.WebsiteUrl))
                .AddScoped<ActivityPubProfileTranslator>()
                .AddScoped<ActivityPubPostTranslator>()
                .AddScoped<ActivityPubRelationshipTranslator>()
                .AddScoped<ActivityPubInteractionTranslator>()
                .AddScoped<IActivityPubCommunicationPrerequisites, ActivityPubCommunicationPrerequisites>()
                .AddScoped<ActivityPubRequestHandler>()
                .AddScoped<ATProtoFeedReader>()
                .AddScoped<ATProtoHandleLookupClient>()
                .AddScoped<CompositeInboxProvider>()
                .AddScoped<CompositeFavoritesProvider>()
                .AddScoped<ConstellationClient>()
                .AddScoped<DeviantArtCredentialProvider>()
                .AddScoped<DIDResolver>()
                .AddScoped<FavoritesFeedBuilder>()
                .AddScoped<FeedBuilder>()
                .AddScoped<IFeedReader, AtomRssFeedReader>()
                .AddScoped<IFeedReader, ESPNContributorFeedReader>()
                .AddScoped<IFeedReader, JsonFeedReader>()
                .AddScoped<IFeedReader, TwtxtFeedReader>()
                .AddScoped<FeedRefresher>()
                .AddScoped<FurryNetworkClient>()
                .AddScoped<JsonLdExpansionService>()
                .AddScoped<LemmyClient>()
                .AddScoped<IMyLinkService, MyLinkService>()
                .AddScoped<PlatformLinkProvider>()
                .AddScoped<WeasylClientFactory>()
                .AddScoped<WebFingerService>();
        }
    }
}
