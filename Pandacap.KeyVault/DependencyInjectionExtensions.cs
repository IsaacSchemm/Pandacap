using Microsoft.Extensions.DependencyInjection;
using Pandacap.ActivityPub.Services.Interfaces;

namespace Pandacap.KeyVault
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddPandacapKeyVault(
            this IServiceCollection serviceCollection,
            Uri keyVaultHost
        ) =>
            serviceCollection
            .AddSingleton(new KeyVaultConfiguration { KeyVaultHost = keyVaultHost })
            .AddScoped<IActivityPubCommunicationPrerequisites, ActivityPubCommunicationPrerequisites>();
    }
}
