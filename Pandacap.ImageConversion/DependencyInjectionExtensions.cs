using Microsoft.Extensions.DependencyInjection;
using Pandacap.ImageConversion.Interfaces;

namespace Pandacap.ImageConversion
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddImageConversion(this IServiceCollection serviceCollection) =>
            serviceCollection.AddScoped<ISvgRenderer, SvgRenderer>();
    }
}
