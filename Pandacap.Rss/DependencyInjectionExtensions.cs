using Microsoft.Extensions.DependencyInjection;
using Pandacap.Rss.Interfaces;

namespace Pandacap.Rss
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
