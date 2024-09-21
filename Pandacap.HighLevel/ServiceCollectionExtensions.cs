﻿using Microsoft.Extensions.DependencyInjection;
using Pandacap.HighLevel.Notifications;
using Pandacap.LowLevel;

namespace Pandacap.HighLevel
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSharedServices(
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
                .AddScoped<ATProtoLikesProvider>()
                .AddScoped<ATProtoNotificationHandler>()
                .AddScoped<BlueskyAgent>()
                .AddScoped<DeviantArtCredentialProvider>()
                .AddScoped<DeviantArtInboxHandler>()
                .AddScoped<DeviantArtFeedNotificationHandler>()
                .AddScoped<DeviantArtNoteNotificationHandler>()
                .AddScoped<FeedBuilder>()
                .AddScoped<IdMapper>()
                .AddScoped<KeyProvider>()
                .AddScoped<OutboxProcessor>()
                .AddScoped<WeasylClientFactory>()
                .AddScoped<WeasylInboxHandler>()
                .AddScoped<WeasylNotificationHandler>();
        }
    }
}
