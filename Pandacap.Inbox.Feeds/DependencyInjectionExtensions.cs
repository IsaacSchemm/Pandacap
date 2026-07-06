using Microsoft.Extensions.DependencyInjection;
using Pandacap.Inbox.Interfaces;

namespace Pandacap.Inbox.Feeds
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddFeedInboxSources(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IInboxSource, FeedInboxSource>();
    }
}
