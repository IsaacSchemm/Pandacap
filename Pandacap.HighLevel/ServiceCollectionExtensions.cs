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
                .AddScoped<ActivityPubNotificationHandler>()
                .AddScoped<ActivityPubRequestHandler>()
                .AddScoped<ActivityPubTranslator>()
                .AddScoped<AtomRssFeedReader>()
                .AddScoped<ATProtoCredentialProvider>()
                .AddScoped<ATProtoInboxHandler>()
                .AddScoped<ATProtoNotificationHandler>()
                .AddScoped<BlueskyAgent>()
                .AddScoped<DeviantArtCredentialProvider>()
                .AddScoped<DeviantArtInboxHandler>()
                .AddScoped<DeviantArtNotificationsHandler>()
                .AddScoped<IdMapper>()
                .AddSingleton(new KeyProvider($"https://{applicationInformation.KeyVaultHostname}"))
                .AddScoped<OutboxProcessor>();
        }
    }
}
