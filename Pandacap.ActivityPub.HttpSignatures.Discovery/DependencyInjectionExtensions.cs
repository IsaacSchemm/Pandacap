using Microsoft.Extensions.DependencyInjection;
using Pandacap.ActivityPub.HttpSignatures.Discovery.Interfaces;

namespace Pandacap.ActivityPub.HttpSignatures.Discovery
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddActivityPubKeyFinder(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IActivityPubKeyFinder, ActivityPubKeyFinder>();
    }
}
