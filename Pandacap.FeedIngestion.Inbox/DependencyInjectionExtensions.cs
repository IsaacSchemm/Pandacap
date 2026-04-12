using Microsoft.Extensions.DependencyInjection;
using Pandacap.FeedIngestion.Inbox.Interfaces;

namespace Pandacap.FeedIngestion.Inbox
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddFeedRefresher(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IFeedRefresher, FeedRefresher>();
    }
}
