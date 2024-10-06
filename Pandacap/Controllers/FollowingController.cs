using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.LowLevel;

namespace Pandacap.Controllers
{
    public class FollowingController(
        ApplicationInformation applicationInformation,
        PandacapDbContext context) : Controller
    {
        public async Task<IActionResult> Bluesky()
        {
            var account = await context.ATProtoCredentials
                .Select(c => new { c.DID })
                .FirstOrDefaultAsync();

            if (account == null)
                return Content("Account not connected.");

            return Redirect($"https://bsky.app/profile/{account.DID}/follows");
        }

        public IActionResult DeviantArt()
        {
            return Redirect($"https://www.deviantart.com/{Uri.EscapeDataString(applicationInformation.DeviantArtUsername)}/about#watching");
        }

        public async Task<IActionResult> Weasyl()
        {
            var credentials = await context.WeasylCredentials
                .Select(c => new { c.Login })
                .FirstOrDefaultAsync();

            if (credentials == null)
                return Content("Account not connected.");

            return Redirect($"https://www.weasyl.com/following/{Uri.EscapeDataString(credentials.Login)}");
        }
    }
}
