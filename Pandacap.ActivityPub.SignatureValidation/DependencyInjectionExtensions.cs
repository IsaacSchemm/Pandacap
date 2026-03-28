using Microsoft.Extensions.DependencyInjection;
using Pandacap.ActivityPub.SignatureValidation.Interfaces;

namespace Pandacap.ActivityPub.SignatureValidation
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddActivityPubKeyAcquisition(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IActivityAuthenticator, ActivityAuthenticator>();
    }
}
