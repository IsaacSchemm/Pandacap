﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    [Authorize]
    public class NotificationsController(
        ActivityPubNotificationHandler activityPubNotificationHandler,
        ATProtoNotificationHandler atProtoNotificationHandler,
        PandacapDbContext context,
        DeviantArtNotificationsHandler deviantArtNotificationsHandler) : Controller
    {
        public async Task<IActionResult> Index()
        {
            int max = 10;

            var activityPub = await activityPubNotificationHandler
                .GetNotificationsAsync()
                .Take(max)
                .ToListAsync();

            var atProto = await atProtoNotificationHandler
                .GetNotificationsAsync()
                .Take(max)
                .ToListAsync();

            var deviantArt = await deviantArtNotificationsHandler
                .GetNotificationsAsync()
                .Take(max)
                .ToListAsync();

            var posts = await context.InboxActivityStreamsPosts
                .Where(post => post.IsMention || post.IsReply)
                .OrderBy(post => post.PostedAt)
                .Take(max)
                .ToListAsync();

            return View(new NotificationsViewModel
            {
                RecentActivityPubActivities = activityPub,
                RecentActivityPubPosts = posts,
                RecentATProtoNotifications = atProto,
                RecentDeviantArtMessages = deviantArt
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DismissActivityPubActivity(IEnumerable<Guid> id)
        {
            var activities = await context.ActivityPubInboundActivities
                .Where(a => id.Contains(a.Id))
                .ToListAsync();

            context.RemoveRange(activities);

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
