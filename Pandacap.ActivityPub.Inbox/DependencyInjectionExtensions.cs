using Microsoft.Extensions.DependencyInjection;
using Pandacap.ActivityPub.Inbox.Interfaces;

namespace Pandacap.ActivityPub.Inbox
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddActivityPubInboxHandler(this IServiceCollection serviceCollection) =>
            serviceCollection.AddScoped<IRemoteActivityPubInboxHandler, RemoteActivityPubInboxHandler>();
    }
}
