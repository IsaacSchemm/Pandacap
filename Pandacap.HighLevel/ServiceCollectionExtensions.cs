using DnsClient;
using Microsoft.Extensions.DependencyInjection;
using Pandacap.ActivityPub.JsonLd;
using Pandacap.ActivityPub.RemoteObjects;
using Pandacap.ActivityPub.Services;
using Pandacap.ATProto.Services;
using Pandacap.FeedIngestion;
using Pandacap.Frontend.ATProto;
using Pandacap.FurAffinity;
using Pandacap.HighLevel.DeviantArt;
using Pandacap.HighLevel.VectorSearch;
using Pandacap.Weasyl.Scraping;

namespace Pandacap.HighLevel
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPandacapServices(
            this IServiceCollection services)
        {
            return services
                .AddSingleton<ILookupClient>(
                    new LookupClient(
                        new LookupClientOptions
                        {
                            UseCache = true
                        }))
                .AddATProtoFeedReader()
                .AddJsonLdExpansionService()
                .AddActivityPubServices()
                .AddActivityPubRemoteObjectServices()
                .AddATProtoServices()
                .AddFeedReaders()
                .AddFurAffinityClient()
                .AddWeasylScraper()
                .AddScoped<CompositeInboxProvider>()
                .AddScoped<CompositeFavoritesProvider>()
                .AddScoped<DeviantArtCredentialProvider>()
                .AddScoped<DIDResolver>()
                .AddScoped<EmbeddingsProvider>()
                .AddScoped<UserAwareClientFactory>()
                .AddScoped<VectorSearchIndexClient>();
        }
    }
}
