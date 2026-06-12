using Microsoft.Extensions.DependencyInjection;
using Pandacap.ActivityPub.InboxRequests.Interfaces;

namespace Pandacap.ActivityPub.InboxRequests
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddActivityPubInboxRequestHandler(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IActivityPubInboxRequestHandler, ActivityPubInboxRequestHandler>();
    }
}
