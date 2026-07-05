using Microsoft.Extensions.DependencyInjection;
using Pandacap.Outbox.FurAffinity;
using Pandacap.Outbox.Interfaces;
using Pandacap.Outbox.Weasyl;

namespace Pandacap.Outbox
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddOutboxDestinations(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IOutboxDestination, FurAffinityOutboxHandler>()
            .AddScoped<IOutboxDestination, WeasylOutboxHandler>();
    }
}
