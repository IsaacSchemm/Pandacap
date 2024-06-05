﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.LowLevel;
using System.Text;

namespace Pandacap.Controllers
{
    [Route("BridgedPosts")]
    public class BridgedPostsController(
        PandacapDbContext context,
        DeviantArtFeedReader feedReader,
        ActivityPubTranslator translator) : Controller
    {
        [Route("{id}")]
        public async Task<IActionResult> Index(Guid id)
        {
            IUserDeviation? post = null;
            post ??= await context.UserArtworkDeviations.Where(p => p.Id == id).SingleOrDefaultAsync();
            post ??= await context.UserTextDeviations.Where(p => p.Id == id).SingleOrDefaultAsync();

            if (post == null)
                return NotFound();

            if (Request.IsActivityPub())
                return Content(
                    ActivityPubSerializer.SerializeWithContext(translator.AsObject(post)),
                    "application/activity+json",
                    Encoding.UTF8);

            return View(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetAltText(Guid id, string alt)
        {
            await feedReader.UpdateAltTextAsync(id, alt);
            return RedirectToAction(nameof(Index), new { id });
        }
    }
}