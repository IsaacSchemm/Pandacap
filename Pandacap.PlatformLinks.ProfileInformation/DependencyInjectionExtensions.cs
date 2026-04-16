using Microsoft.Extensions.DependencyInjection;
using Pandacap.PlatformLinks.ProfileInformation.Interfaces;

namespace Pandacap.PlatformLinks.ProfileInformation
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddProfileInformationProvider(this IServiceCollection serviceCollection) =>
            serviceCollection.AddScoped<IProfileInformationProvider, ProfileInformationProvider>();
    }
}
