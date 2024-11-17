using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.JsonLd;
using Pandacap.LowLevel;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    public class CommunitiesController(
        PandacapDbContext context
    ) : Controller
    {
        public IActionResult ViewCommunity(
            string actorId,
            int page = 1,
            CancellationToken cancellationToken = default)
        {
            return Redirect(actorId);
        }

        public async Task<IActionResult> Bookmarks()
        {
            var communityBookmarks = await context.CommunityBookmarks.ToListAsync();

            return View(communityBookmarks
                .OrderBy(c => c.PreferredUsername)
                .ThenBy(c => c.ActorId));
        }

    }
}
