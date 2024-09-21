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
        DeviantArtFeedNotificationHandler deviantArtFeedNotificationHandler,
        DeviantArtNoteNotificationHandler deviantArtNoteNotificationHandler,
        WeasylNotificationHandler weasylNotificationHandler) : Controller
    {
        private static readonly DateTimeOffset Cutoff = DateTimeOffset.UtcNow.AddDays(-30);

        private async Task<IReadOnlyList<Notification>> CollectNotificationsAsync() =>
            await new[]
            {
                activityPubReplyHandler.GetNotificationsAsync(),
                activityPubNotificationHandler.GetUserPostNotificationsAsync(),
                activityPubNotificationHandler.GetAddressedPostNotificationsAsync(),
                atProtoNotificationHandler.GetNotificationsAsync(),
                deviantArtFeedNotificationHandler.GetNotificationsAsync(),
                deviantArtNoteNotificationHandler.GetNotificationsAsync(),
                weasylNotificationHandler.GetNotificationsAsync()
            }
            .MergeNewest(post => post.Timestamp)
            .TakeWhile(post => post.Timestamp > Cutoff)
            .ToListAsync();

        public async Task<IActionResult> ByDate() =>
            View(await CollectNotificationsAsync());

        public async Task<IActionResult> ByPlatform() =>
            View(await CollectNotificationsAsync());
    }
}
