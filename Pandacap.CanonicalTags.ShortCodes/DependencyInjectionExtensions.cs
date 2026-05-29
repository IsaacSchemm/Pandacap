using Microsoft.Extensions.DependencyInjection;
using Pandacap.CanonicalTags.ShortCodes.Interfaces;

namespace Pandacap.CanonicalTags.ShortCodes
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddCanonicalTagShortCodeService(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<ICanonicalTagShortCodeService, CanonicalTagShortCodeService>();
    }
}
