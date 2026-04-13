using Microsoft.Extensions.DependencyInjection;
using Pandacap.Inbox.Feeds.Interfaces;
using Pandacap.Inbox.Interfaces;

namespace Pandacap.Inbox.Feeds
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddFeedRefresher(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IFeedRefresher, FeedRefresher>()
            .AddScoped<IInboxSourceFactory, FeedInboxSourceFactory>();
    }
}
