using Microsoft.Extensions.DependencyInjection;
using Pandacap.FeedCreation.Outbox.Interfaces;

namespace Pandacap.FeedCreation.Outbox
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddFeedBuilder(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IFeedBuilder, FeedBuilder>();
    }
}
