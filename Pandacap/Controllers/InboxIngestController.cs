﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;

namespace Pandacap.Controllers
{
    public class InboxIngestController(
        AtomRssFeedReader atomRssFeedReader,
        ATProtoInboxHandler atProtoInboxHandler,
        PandacapDbContext context,
        DeviantArtInboxHandler deviantArtInboxHandler) : Controller
    {
        public async Task<IActionResult> DeviantArtArtworkPosts()
        {
            await deviantArtInboxHandler.ImportArtworkPostsByUsersWeWatchAsync();
            return RedirectToAction("ImagePosts", "Inbox");
        }

        public async Task<IActionResult> DeviantArtTextPosts()
        {
            await deviantArtInboxHandler.ImportTextPostsByUsersWeWatchAsync();
            return RedirectToAction("TextPosts", "Inbox");
        }

        public async Task<IActionResult> Feed()
        {
            var feedIds = await context.RssFeeds.Select(f => f.Id).ToListAsync();
            await Task.WhenAll(feedIds.Select(atomRssFeedReader.ReadFeedAsync));
            return NoContent();
        }

        public async Task<IActionResult> BlueskyTimeline()
        {
            await atProtoInboxHandler.ImportPostsByUsersWeWatchAsync();
            return NoContent();
        }
    }
}