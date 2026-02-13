using Microsoft.Azure.Functions.Worker;
using Pandacap.Data;
using Pandacap.HighLevel;

namespace Pandacap.Functions
{
    public class LongTermInboxCleanup(
        CompositeInboxProvider compositeInboxProvider,
        PandacapDbContext context)
    {
        [Function("LongTermInboxCleanup")]
        public async Task Run([TimerTrigger("0 5 9 * * *")] TimerInfo myTimer)
        {
            var cutoff = DateTimeOffset.UtcNow.AddDays(-30);

            var oldPosts = compositeInboxProvider.GetAllInboxPostsAsync()
                .Skip(200)
                .SkipWhile(post => post.PostedAt > cutoff);

            await foreach (var post in oldPosts)
                post.DismissedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync();
        }
    }
}
