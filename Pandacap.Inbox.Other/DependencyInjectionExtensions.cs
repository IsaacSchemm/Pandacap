using Microsoft.Extensions.DependencyInjection;
using Pandacap.Inbox.Interfaces;
using Pandacap.Inbox.Other.DeviantArt;
using Pandacap.Inbox.Other.FurAffinity;
using Pandacap.Inbox.Other.Weasyl;

namespace Pandacap.Inbox.Other
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddOtherInboxSources(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IInboxSource, DeviantArtInboxHandler>()
            .AddScoped<IInboxSource, FurAffinityInboxHandler>()
            .AddScoped<IInboxSource, WeasylInboxHandler>();
    }
}
