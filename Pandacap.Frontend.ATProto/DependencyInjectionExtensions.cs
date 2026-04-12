using Microsoft.Extensions.DependencyInjection;
using Pandacap.Frontend.ATProto.Interfaces;

namespace Pandacap.Frontend.ATProto
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddATProtoFeedReader(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IATProtoFeedReader, ATProtoFeedReader>();
    }
}
