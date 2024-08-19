using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pandacap.HighLevel;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    [Authorize]
    public class NotificationsController(
        ActivityPubNotificationHandler activityPubNotificationHandler,
        ATProtoNotificationHandler atProtoNotificationHandler,
        DeviantArtNotificationsHandler deviantArtNotificationsHandler) : Controller
    {
        public async Task<IActionResult> Index()
        {
            int max = 10;
            var cutoff = DateTimeOffset.UtcNow.AddDays(-90);

            var activityPub = await activityPubNotificationHandler
                .GetNotificationsAsync()
                .TakeWhile(n => n.RemoteActivity.AddedAt > cutoff)
                .Take(max)
                .ToListAsync();

            var atProto = await atProtoNotificationHandler
                .GetNotificationsAsync()
                .TakeWhile(n => n.IndexedAt > cutoff)
                .Take(max)
                .ToListAsync();

            var deviantArt = await deviantArtNotificationsHandler
                .GetNotificationsAsync()
                .Take(max)
                .ToListAsync();

            return View(new NotificationsViewModel
            {
                RecentActivities = activityPub,
                RecentATProtoNotifications = atProto,
                RecentMessages = deviantArt
            });
        }
    }
}
