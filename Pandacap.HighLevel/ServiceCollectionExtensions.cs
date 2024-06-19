using Microsoft.Extensions.DependencyInjection;
using Pandacap.HighLevel.ActivityPub;
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
                .AddScoped<ActivityPubTranslator>()
                .AddScoped<DeviantArtCredentialProvider>()
                .AddScoped<DeviantArtHandler>()
                .AddScoped<IdMapper>()
                .AddScoped<ImageProxy>()
                .AddSingleton(new KeyProvider($"https://{applicationInformation.KeyVaultHostname}"))
                .AddScoped<OutboxProcessor>()
                .AddScoped<RemoteActivityPubPostHandler>()
                .AddScoped<RemoteActorFetcher>();
        }
    }
}
