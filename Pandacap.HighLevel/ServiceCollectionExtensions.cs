﻿using Microsoft.Extensions.DependencyInjection;
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
                .AddScoped<ATProtoCredentialProvider>()
                .AddScoped<ATProtoInboxHandler>()
                .AddScoped<ActivityPubRequestHandler>()
                .AddScoped<ActivityPubTranslator>()
                .AddScoped<DeviantArtCredentialProvider>()
                .AddScoped<DeviantArtInboxHandler>()
                .AddScoped<IdMapper>()
                .AddScoped<InboxIngestion>()
                .AddSingleton(new KeyProvider($"https://{applicationInformation.KeyVaultHostname}"))
                .AddScoped<OutboxProcessor>();
        }
    }
}
