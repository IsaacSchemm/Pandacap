using Microsoft.Extensions.DependencyInjection;
using Pandacap.ActivityPub.Outbox.Interfaces;

namespace Pandacap.ActivityPub.Outbox
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddActivityPubOutboxProcessor(this IServiceCollection serviceCollection) =>
            serviceCollection.AddScoped<IActivityPubOutboxProcessor, OutboxProcessor>();
    }
}
