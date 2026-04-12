using Microsoft.Extensions.DependencyInjection;
using Pandacap.ActivityPub.Services.Interfaces;

namespace Pandacap.KeyVault
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddPandacapKeyVault(
            this IServiceCollection serviceCollection,
            KeyVaultConfiguration keyVaultConfiguration
        ) =>
            serviceCollection
            .AddSingleton(keyVaultConfiguration)
            .AddScoped<IActivityPubCommunicationPrerequisites, ActivityPubCommunicationPrerequisites>();
    }
}
