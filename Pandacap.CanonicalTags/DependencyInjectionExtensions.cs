using Microsoft.Extensions.DependencyInjection;
using Pandacap.CanonicalTags.Interfaces;

namespace Pandacap.CanonicalTags
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddCanonicalTagServices(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<ICanonicalTagImplicationService, CanonicalTagImplicationService>()
            .AddScoped<ICanonicalTagShortCodeService, CanonicalTagShortCodeService>();
    }
}
