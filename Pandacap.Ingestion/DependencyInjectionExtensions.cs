using Microsoft.Extensions.DependencyInjection;
using Pandacap.Ingestion.Interfaces;

namespace Pandacap.Ingestion
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddIngestion(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IATProtoFeedRefresher, ATProtoFeedRefresher>()
            .AddScoped<IFeedRefresher, FeedRefresher>();
    }
}
