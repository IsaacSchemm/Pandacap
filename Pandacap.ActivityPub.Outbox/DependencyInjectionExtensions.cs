using Microsoft.Extensions.DependencyInjection;
using Pandacap.ActivityPub.Outbox.Interfaces;

namespace Pandacap.ActivityPub.Outbox
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddActivityPubOutboxServices(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IActivityPubOutboxProcessor, OutboxProcessor>()
            .AddScoped<IDeliveryInboxCollector, DeliveryInboxCollector>();
    }
}
