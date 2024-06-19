using Microsoft.Extensions.DependencyInjection;
using Pandacap.LowLevel;

namespace Pandacap.HighLevel
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPandacapServices(
            this IServiceCollection services,
            ApplicationInformation applicationInformation)
        {
            return services
                .AddSingleton(applicationInformation)
                .AddScoped<AltTextSentinel>()
                .AddScoped<AtomRssFeedReader>()
                .AddScoped<ActivityPubRequestHandler>()
                .AddScoped<ActivityPubTranslator>()
                .AddScoped<DeviantArtCredentialProvider>()
                .AddScoped<DeviantArtInboxHandler>()
                .AddScoped<DeviantArtHandler>()
                .AddScoped<IdMapper>()
                .AddSingleton(new KeyProvider($"https://{applicationInformation.KeyVaultHostname}"))
                .AddScoped<OutboxProcessor>()
                .AddScoped<RemoteActivityPubPostHandler>();
        }
    }
}
