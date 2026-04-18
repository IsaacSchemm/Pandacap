using Microsoft.Extensions.DependencyInjection;
using Pandacap.ActivityPub.Replies.Interfaces;

namespace Pandacap.ActivityPub.Replies
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddReplyCollationService(this IServiceCollection serviceCollection) =>
            serviceCollection.AddScoped<IReplyCollationService, ReplyCollationService>();
    }
}
