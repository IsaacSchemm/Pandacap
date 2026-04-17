using Microsoft.Extensions.DependencyInjection;
using Pandacap.FeedIngestion.Interfaces;

namespace Pandacap.FeedIngestion
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddFeedReaders(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IFeedReader, AtomRssFeedReader>()
            .AddScoped<IFeedReader, JsonFeedReader>()
            .AddScoped<IFeedReader, TwtxtFeedReader>()
            .AddScoped<IFeedRequestHandler, FeedRequestHandler>();
    }
}
