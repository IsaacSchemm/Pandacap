using Microsoft.Extensions.DependencyInjection;
using Pandacap.ManualInboxIngestion.ATProto.Interfaces;

namespace Pandacap.ManualInboxIngestion.ATProto
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddATProtoFeedRefresher(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IATProtoFeedRefresher, ATProtoFeedRefresher>();
    }
}
