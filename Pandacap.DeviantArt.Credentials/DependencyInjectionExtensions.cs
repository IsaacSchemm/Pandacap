using Microsoft.Extensions.DependencyInjection;
using Pandacap.DeviantArt.Credentials.Interfaces;

namespace Pandacap.DeviantArt.Credentials
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddDeviantArtCredentials(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IDeviantArtCredentialProvider, DeviantArtCredentialProvider>();
    }
}
