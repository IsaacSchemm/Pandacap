using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pandacap.HighLevel;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    [Authorize]
    public class NotificationsController(
        ActivityPubNotificationHandler activityPubNotificationHandler,
        ATProtoNotificationHandler atProtoNotificationHandler) : Controller
    {
        public async Task<IActionResult> Index()
        {
            int max = 5;

            var activityPubTask = activityPubNotificationHandler
                .GetNotificationsAsync()
                .Take(max)
                .ToListAsync();

            var atProtoTask = atProtoNotificationHandler
                .GetNotificationsAsync()
                .Take(max)
                .ToListAsync();

            return View(new NotificationsViewModel
            {
                RecentActivities = await activityPubTask,
                RecentATProtoNotifications = await atProtoTask
            });
        }
    }
}
