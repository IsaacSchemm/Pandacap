using Microsoft.Extensions.DependencyInjection;
using Pandacap.Notifications.Interfaces;

namespace Pandacap.Notifications
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddNotificationHandlers(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<INotificationHandler, ActivityPubAddressedPostNotificationHandler>()
            .AddScoped<INotificationHandler, ActivityPubNotificationHandler>()
            .AddScoped<INotificationHandler, ActivityPubReplyNotificationHandler>()
            .AddScoped<INotificationHandler, ATProtoNotificationHandler>()
            .AddScoped<INotificationHandler, DeviantArtFeedNotificationHandler>()
            .AddScoped<INotificationHandler, DeviantArtNoteNotificationHandler>()
            .AddScoped<INotificationHandler, FurAffinityNoteNotificationHandler>()
            .AddScoped<INotificationHandler, FurAffinityNotificationHandler>()
            .AddScoped<INotificationHandler, WeasylNoteNotificationHandler>()
            .AddScoped<INotificationHandler, WeasylNotificationHandler>();
    }
}
