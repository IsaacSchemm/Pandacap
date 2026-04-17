using Microsoft.Extensions.DependencyInjection;
using Pandacap.ActivityPub.JsonLd.Interfaces;

namespace Pandacap.ActivityPub.JsonLd
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddJsonLdExpansionService(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IJsonLdExpansionService, JsonLdExpansionService>();
    }
}
