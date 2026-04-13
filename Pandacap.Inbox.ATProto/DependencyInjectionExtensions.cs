using Microsoft.Extensions.DependencyInjection;
using Pandacap.Inbox.ATProto.Interfaces;

namespace Pandacap.Inbox.ATProto
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddATProtoFeedReader(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IATProtoFeedReader, ATProtoFeedReader>();
    }
}
