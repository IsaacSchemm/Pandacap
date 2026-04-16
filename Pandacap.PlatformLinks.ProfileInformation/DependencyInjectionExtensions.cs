using Microsoft.Extensions.DependencyInjection;
using Pandacap.PlatformLinks.ProfileInformation.Interfaces;

namespace Pandacap.PlatformLinks.ProfileInformation
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddPlatformLinkProfileInformationProvider(this IServiceCollection serviceCollection) =>
            serviceCollection.AddScoped<IPlatformLinkProfileInformationProvider, PlatformLinkProfileInformationProvider>();
    }
}
