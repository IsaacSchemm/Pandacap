using Microsoft.EntityFrameworkCore;
using Pandacap.Data;

namespace Pandacap.HighLevel.Notifications
{
    public class ActivityPubReplyHandler(PandacapDbContext context)
    {
        public IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            return context.InboxActivityStreamsPosts
                .Where(post => post.IsMention || post.IsReply)
                .Where(post => post.DismissedAt == null)
                .OrderByDescending(post => post.PostedAt)
                .Select(post => new Notification
                {
                    Platform = "ActivityPub (mentions & replies)",
                    ActivityName = "New post",
                    UserName = post.Author.Username,
                    UserUrl = post.Author.Id,
                    Timestamp = post.PostedAt,
                    Url = $"/Inbox/Single?id={post.Id}"
                })
                .AsAsyncEnumerable();
        }
    }
}
