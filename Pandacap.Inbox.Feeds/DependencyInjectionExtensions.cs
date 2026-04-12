using Microsoft.Extensions.DependencyInjection;
using Pandacap.Inbox.Feeds.Interfaces;

namespace Pandacap.Inbox.Feeds
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddFeedRefresher(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IFeedRefresher, FeedRefresher>();
    }
}
