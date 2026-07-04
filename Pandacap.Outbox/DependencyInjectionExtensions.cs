using Microsoft.Extensions.DependencyInjection;

namespace Pandacap.Outbox
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddOutboxHandlers(this IServiceCollection serviceCollection) =>
            serviceCollection;
    }
}
