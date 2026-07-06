using Microsoft.Extensions.DependencyInjection;
using Pandacap.Inbox.Interfaces;

namespace Pandacap.Inbox
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddInboxSources(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IInboxSource, ATProtoInboxSource>()
            .AddScoped<IInboxSource, DeviantArtInboxHandler>()
            .AddScoped<IInboxSource, FeedInboxSource>()
            .AddScoped<IInboxSource, FurAffinityInboxHandler>()
            .AddScoped<IInboxSource, WeasylInboxHandler>();
    }
}
