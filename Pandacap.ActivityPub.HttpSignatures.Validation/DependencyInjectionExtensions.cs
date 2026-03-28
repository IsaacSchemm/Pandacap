using Microsoft.Extensions.DependencyInjection;
using Pandacap.ActivityPub.HttpSignatures.Validation.Interfaces;

namespace Pandacap.ActivityPub.HttpSignatures.Validation
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddActivityPubSignatureValidator(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IActivityPubSignatureValidator, MastodonVerifier>();
    }
}
