using Microsoft.Extensions.DependencyInjection;
using Pandacap.Frontend.Feeds.Interfaces;

namespace Pandacap.Frontend.Feeds
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddFeedBuilder(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IFeedBuilder, FeedBuilder>();
    }
}
