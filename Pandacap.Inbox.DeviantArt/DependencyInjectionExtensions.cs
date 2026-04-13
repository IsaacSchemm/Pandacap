using Microsoft.Extensions.DependencyInjection;
using Pandacap.Inbox.Interfaces;

namespace Pandacap.Inbox.DeviantArt
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddDeviantArtInboxHandlers(this IServiceCollection serviceCollection) =>
            serviceCollection.AddScoped<IInboxSource, DeviantArtInboxHandler>();
    }
}
