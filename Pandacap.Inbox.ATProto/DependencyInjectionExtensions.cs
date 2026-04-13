using Microsoft.Extensions.DependencyInjection;
using Pandacap.Inbox.ATProto.Interfaces;
using Pandacap.Inbox.Interfaces;

namespace Pandacap.Inbox.ATProto
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddATProtoInboxHandlers(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IATProtoFeedReader, ATProtoFeedReader>()
            .AddScoped<IInboxSourceFactory, ATProtoInboxSourceFactory>();
    }
}
