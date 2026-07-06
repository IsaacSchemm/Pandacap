using Microsoft.Extensions.DependencyInjection;
using Pandacap.Inbox.Interfaces;

namespace Pandacap.Inbox.ATProto
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddATProtoInboxSources(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IInboxSource, ATProtoInboxSource>();
    }
}
