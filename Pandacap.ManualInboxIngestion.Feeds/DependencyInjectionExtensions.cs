using Microsoft.Extensions.DependencyInjection;
using Pandacap.ManualInboxIngestion.Feeds.Interfaces;

namespace Pandacap.ManualInboxIngestion.Feeds
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddFeedRefresher(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IFeedRefresher, FeedRefresher>();
    }
}
