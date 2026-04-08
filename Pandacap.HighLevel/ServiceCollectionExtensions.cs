using DnsClient;
using Microsoft.Extensions.DependencyInjection;
using Pandacap.ActivityPub.JsonLd;
using Pandacap.ActivityPub.RemoteObjects;
using Pandacap.ActivityPub.Services;
using Pandacap.ActivityPub.Services.Interfaces;
using Pandacap.ActivityPub.Static;
using Pandacap.ATProto.Services;
using Pandacap.ConfigurationObjects;
using Pandacap.FeedIngestion;
using Pandacap.FurAffinity;
using Pandacap.HighLevel.ATProto;
using Pandacap.HighLevel.DeviantArt;
using Pandacap.HighLevel.FeedReaders;
using Pandacap.HighLevel.RssOutbound;
using Pandacap.HighLevel.VectorSearch;
using Pandacap.Weasyl;
using Pandacap.Weasyl.Scraping;

namespace Pandacap.HighLevel
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPandacapServices(
            this IServiceCollection services,
            ApplicationInformation appInfo)
        {
            ActivityPubHostInformation.ApplicationHostname = appInfo.ApplicationHostname;
            ActivityPubHostInformation.Username = appInfo.Username;

            return services
                .AddSingleton<ILookupClient>(
                    new LookupClient(
                        new LookupClientOptions
                        {
                            UseCache = true
                        }))
                .AddSingleton(appInfo)
                .AddJsonLdExpansionService()
                .AddActivityPubServices()
                .AddActivityPubRemoteObjectServices()
                .AddATProtoServices()
                .AddFeedReaders()
                .AddFurAffinityClient()
                .AddWeasylClient()
                .AddWeasylScraper()
                .AddScoped<IActivityPubCommunicationPrerequisites, ActivityPubCommunicationPrerequisites>()
                .AddScoped<ATProtoFeedReader>()
                .AddScoped<ATProtoHandleLookupClient>()
                .AddScoped<CompositeInboxProvider>()
                .AddScoped<CompositeFavoritesProvider>()
                .AddScoped<DeviantArtCredentialProvider>()
                .AddScoped<DIDResolver>()
                .AddScoped<EmbeddingsProvider>()
                .AddScoped<FavoritesFeedBuilder>()
                .AddScoped<FeedBuilder>()
                .AddScoped<FeedRefresher>()
                .AddScoped<UserAwareClientFactory>()
                .AddScoped<VectorSearchIndexClient>();
        }
    }
}
