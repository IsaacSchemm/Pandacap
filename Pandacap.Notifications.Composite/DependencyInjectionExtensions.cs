using Microsoft.Extensions.DependencyInjection;
using Pandacap.Notifications.Composite.Interfaces;

namespace Pandacap.Notifications.Composite
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddCompositeNotificationHandler(this IServiceCollection serviceCollection) =>
            serviceCollection.AddScoped<ICompositeNotificationHandler, CompositeNotificationHandler>();
    }
}
