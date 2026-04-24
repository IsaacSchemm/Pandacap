using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Notifications.Composite.Interfaces;
using Pandacap.Notifications.Interfaces;

namespace Pandacap.Controllers
{
    [Authorize]
    public class NotificationsController(ICompositeNotificationHandler notificationHandler) : Controller
    {
        private static readonly DateTimeOffset Cutoff = DateTimeOffset.UtcNow.AddDays(-90);

        private async Task<IReadOnlyList<INotification>> CollectNotificationsAsync(CancellationToken cancellationToken) =>
            await notificationHandler
            .GetNotificationsAsync()
            .TakeWhile(post => post.Timestamp > Cutoff)
            .ToListAsync(cancellationToken);

        public async Task<IActionResult> ByDate(CancellationToken cancellationToken) =>
            View(await CollectNotificationsAsync(cancellationToken));

        public async Task<IActionResult> ByPlatform(CancellationToken cancellationToken) =>
            View(await CollectNotificationsAsync(cancellationToken));
    }
}
