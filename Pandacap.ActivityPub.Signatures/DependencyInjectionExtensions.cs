using Microsoft.Extensions.DependencyInjection;
using Pandacap.ActivityPub.Signatures.Interfaces;

namespace Pandacap.ActivityPub.Signatures
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddActivityPubKeyVerification(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IMastodonVerifier, MastodonVerifier>();
    }
}
