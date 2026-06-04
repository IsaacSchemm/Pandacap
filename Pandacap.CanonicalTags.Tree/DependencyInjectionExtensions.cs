using Microsoft.Extensions.DependencyInjection;
using Pandacap.CanonicalTags.Tree.Interfaces;

namespace Pandacap.CanonicalTags.Tree
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddCanonicalTagTreeService(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<ICanonicalTagTreeService, CanonicalTagTreeService>();
    }
}
