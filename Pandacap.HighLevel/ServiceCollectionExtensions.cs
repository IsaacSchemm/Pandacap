using DnsClient;
using Microsoft.Extensions.DependencyInjection;
using Pandacap.ActivityPub.JsonLd;
using Pandacap.ActivityPub.RemoteObjects;
using Pandacap.ActivityPub.Services;
using Pandacap.ATProto.Services;
using Pandacap.Credentials;
using Pandacap.FeedIngestion;
using Pandacap.FurAffinity;
using Pandacap.Inbox.ATProto;
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
                .AddActivityPubServices()
                .AddActivityPubRemoteObjectServices()
                .AddATProtoServices()
                .AddCredentialProviders()
                .AddFeedReaders()
                .AddFurAffinityClient()
                .AddJsonLdExpansionService()
                .AddWeasylScraper();
        }
    }
}
