using Microsoft.Extensions.DependencyInjection;
using Pandacap.Inbox.Interfaces;

namespace Pandacap.Inbox
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddInboxPopulator(this IServiceCollection serviceCollection) =>
            serviceCollection.AddScoped<IInboxPopulator, InboxPopulator>();
    }
}
