using Microsoft.Extensions.DependencyInjection;
using Pandacap.PeriodicTasks.Interfaces;

namespace Pandacap.PeriodicTasks
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddPeriodicTaskServices(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<ICleanupService, CleanupService>();
    }
}
