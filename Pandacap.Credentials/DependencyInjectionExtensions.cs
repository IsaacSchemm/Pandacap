using Microsoft.Extensions.DependencyInjection;
using Pandacap.Credentials.Interfaces;

namespace Pandacap.Credentials
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddCredentialProviders(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IDeviantArtCredentialProvider, DeviantArtCredentialProvider>()
            .AddScoped<IUserAwareWeasylClientFactory, UserAwareWeasylClientFactory>();
    }
}
