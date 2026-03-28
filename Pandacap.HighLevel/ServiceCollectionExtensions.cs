using DnsClient;
using Microsoft.Extensions.DependencyInjection;
using Pandacap.ActivityPub.RemoteObjects;
using Pandacap.ActivityPub.RemoteObjects.Interfaces;
using Pandacap.ActivityPub.Services;
using Pandacap.ActivityPub.Services.Interfaces;
using Pandacap.ActivityPub.Static;
using Pandacap.Clients.ATProto;
using Pandacap.ConfigurationObjects;
using Pandacap.FurAffinity;
using Pandacap.FurAffinity.Interfaces;
using Pandacap.HighLevel.ATProto;
using Pandacap.HighLevel.DeviantArt;
using Pandacap.HighLevel.FeedReaders;
using Pandacap.HighLevel.Lemmy;
using Pandacap.HighLevel.PlatformLinks;
using Pandacap.HighLevel.Resolvers;
using Pandacap.HighLevel.RssOutbound;
using Pandacap.HighLevel.VectorSearch;
using Pandacap.Weasyl;
using Pandacap.Weasyl.Interfaces;
using Pandacap.Weasyl.Scraping;
using Pandacap.Weasyl.Scraping.Interfaces;

namespace Pandacap.HighLevel
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPandacapServices(
            this IServiceCollection services,
            ApplicationInformation appInfo)
        {
            ActivityPubHostInformation.ApplicationHostname = appInfo.ApplicationHostname;

            return services
                .AddSingleton<ILookupClient>(
                    new LookupClient(
                        new LookupClientOptions
                        {
                            UseCache = true
                        }))
                .AddSingleton(appInfo)
                .AddActivityPubServices()
                .AddActivityPubRemoteObjectServices()
                .AddFurAffinityClient()
                .AddWeasylClient()
                .AddWeasylScraper()
                .AddScoped<IActivityPubCommunicationPrerequisites, ActivityPubCommunicationPrerequisites>()
                .AddScoped<ATProtoFeedReader>()
                .AddScoped<ATProtoHandleLookupClient>()
                .AddScoped<CompositeInboxProvider>()
                .AddScoped<CompositeFavoritesProvider>()
                .AddScoped<CompositeResolver>()
                .AddScoped<ConstellationClient>()
                .AddScoped<DeviantArtCredentialProvider>()
                .AddScoped<DIDResolver>()
                .AddScoped<EmbeddingsProvider>()
                .AddScoped<FavoritesFeedBuilder>()
                .AddScoped<FeedBuilder>()
                .AddScoped<IFeedReader, AtomRssFeedReader>()
                .AddScoped<IFeedReader, JsonFeedReader>()
                .AddScoped<IFeedReader, TwtxtFeedReader>()
                .AddScoped<FeedRefresher>()
                .AddScoped<LemmyClient>()
                .AddScoped<PlatformIconProvider>()
                .AddScoped<PlatformLinkProvider>()
                .AddScoped<IResolver, ActivityPubResolver>()
                .AddScoped<IResolver, ATUriResolver>()
                .AddScoped<IResolver, ATUriResolver>()
                .AddScoped<IResolver, BlueskyAppViewPostResolver>()
                .AddScoped<IResolver, BlueskyAppViewProfileResolver>()
                .AddScoped<IResolver, BlueskyHandleResolver>()
                .AddScoped<IResolver, WebFingerResolver>()
                .AddScoped<UserAwareClientFactory>()
                .AddScoped<VectorSearchIndexClient>();
        }
    }
}
