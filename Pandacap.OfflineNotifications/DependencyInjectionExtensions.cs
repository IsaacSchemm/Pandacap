using Microsoft.Extensions.DependencyInjection;
using Pandacap.OfflineNotifications.Interfaces;

namespace Pandacap.OfflineNotifications
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddOfflineNotificationsSources(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IOfflineNotificationsSource, FurAffinityOfflineNotificationsSource>()
            .AddScoped<IOfflineNotificationsSource, WeasylOfflineNotificationsSource>();
    }
}
