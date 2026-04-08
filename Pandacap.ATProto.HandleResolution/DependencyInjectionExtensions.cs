using Microsoft.Extensions.DependencyInjection;
using Pandacap.ATProto.HandleResolution.Interfaces;

namespace Pandacap.ATProto.HandleResolution
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddATProtoHandleResolution(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IATProtoHandleLookupClient, ATProtoHandleLookupClient>();
    }
}
