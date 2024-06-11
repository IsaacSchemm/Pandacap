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
                .AddScoped<AtomRssFeedReader>()
                .AddScoped<ActivityPubTranslator>()
                .AddScoped<DeviantArtFeedReader>()
                .AddScoped<IdMapper>()
                .AddScoped<ImageProxy>()
                .AddSingleton(new KeyProvider($"https://{applicationInformation.KeyVaultHostname}"))
                .AddScoped<RemoteActivityPubPostHandler>()
                .AddScoped<RemoteActorFetcher>();
        }
    }
}
