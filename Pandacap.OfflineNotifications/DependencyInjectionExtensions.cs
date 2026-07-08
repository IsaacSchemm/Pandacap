using Microsoft.Extensions.DependencyInjection;
using Pandacap.OfflineNotifications.Interfaces;

namespace Pandacap.OfflineNotifications
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddOfflineNotificationsSources(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IOfflineNotificationsSource, FurAffinityOfflineNoteNotificationsSource>()
            .AddScoped<IOfflineNotificationsSource, FurAffinityOfflineNotificationsSource>()
            .AddScoped<IOfflineNotificationsSource, WeasylOfflineNoteNotificationSource>()
            .AddScoped<IOfflineNotificationsSource, WeasylOfflineNotificationsSource>();
    }
}
