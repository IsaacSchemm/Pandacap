using Microsoft.Extensions.DependencyInjection;
using Pandacap.Resolvers.Interfaces;

namespace Pandacap.Resolvers
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddResolvers(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<ICompositeResolver, CompositeResolver>()
            .AddScoped<IResolver, ActivityPubResolver>()
            .AddScoped<IResolver, ATUriResolver>()
            .AddScoped<IResolver, BlueskyAppViewPostResolver>()
            .AddScoped<IResolver, BlueskyAppViewProfileResolver>()
            .AddScoped<IResolver, BlueskyHandleResolver>()
            .AddScoped<IResolver, WebFingerResolver>();
    }
}
