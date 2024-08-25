using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.HighLevel.Notifications;

namespace Pandacap.Controllers
{
    [Authorize]
    public class NotificationsController(
        ActivityPubNotificationHandler activityPubNotificationHandler,
        ActivityPubReplyHandler activityPubReplyHandler,
        ATProtoNotificationHandler atProtoNotificationHandler,
        DeviantArtNotificationHandler deviantArtNotificationsHandler) : Controller
    {
        public async Task<IActionResult> Index()
        {
            DateTimeOffset cutoff = DateTimeOffset.UtcNow.AddDays(-30);

            var activityPubPosts = await activityPubReplyHandler
                .GetNotificationsAsync()
                .TakeWhile(post => post.Timestamp > cutoff)
                .ToListAsync();

            var activityPubActivities = await activityPubNotificationHandler
                .GetNotificationsAsync()
                .TakeWhile(post => post.Timestamp > cutoff)
                .ToListAsync();

            var atProto = await atProtoNotificationHandler
                .GetNotificationsAsync()
                .TakeWhile(post => post.Timestamp > cutoff)
                .ToListAsync();

            var deviantArt = await deviantArtNotificationsHandler
                .GetNotificationsAsync()
                .TakeWhile(post => post.Timestamp > cutoff)
                .ToListAsync();

            var all = Enumerable.Empty<Notification>()
                .Concat(activityPubPosts)
                .Concat(activityPubActivities)
                .Concat(atProto)
                .Concat(deviantArt);

            return View(all);
        }
    }
}
