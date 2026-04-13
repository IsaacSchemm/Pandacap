using Microsoft.Extensions.DependencyInjection;
using Pandacap.Inbox.ATProto;
using Pandacap.Inbox.DeviantArt;
using Pandacap.Inbox.Feeds;
using Pandacap.Inbox.FurAffinity;
using Pandacap.Inbox.Interfaces;
using Pandacap.Inbox.Weasyl;

namespace Pandacap.Inbox
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddInboxHandlers(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IATProtoFeedReader, ATProtoFeedReader>()
            .AddScoped<IFeedRefresher, FeedRefresher>()
            .AddScoped<IInboxPopulator, InboxPopulator>()
            .AddScoped<IInboxSourceFactory, ATProtoInboxSourceFactory>()
            .AddScoped<IInboxSourceFactory, DeviantArtInboxHandler>()
            .AddScoped<IInboxSourceFactory, FeedInboxSourceFactory>()
            .AddScoped<IInboxSourceFactory, FurAffinityInboxHandler>()
            .AddScoped<IInboxSourceFactory, WeasylInboxHandler>();
    }
}
