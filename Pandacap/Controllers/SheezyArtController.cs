using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub;
using Pandacap.Data;
using Pandacap.Html;

namespace Pandacap.Controllers
{
    public class SheezyArtController(
        PandacapDbContext context,
        Mapper mapper) : Controller
    {
        public async Task<IActionResult> Setup(CancellationToken cancellationToken)
        {
            var account = await context.SheezyArtAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            ViewBag.Username = account?.Username;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(string username, CancellationToken cancellationToken)
        {
            var profile = await SheezyArtScraper.GetProfileAsync(username);

            if (!Uri.TryCreate(mapper.ActorId, UriKind.Absolute, out Uri? actorId)
                || !profile.socialLinks.Contains(actorId))
            {
                return Content("No social link to your Pandacap actor ID was found on the specified Sheezy.Art profile.");
            }

            var accounts = await context.SheezyArtAccounts.ToListAsync(cancellationToken);
            context.RemoveRange(accounts);

            context.SheezyArtAccounts.Add(new()
            {
                Username = username
            });

            await context.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(Setup));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reset(CancellationToken cancellationToken)
        {
            var accounts = await context.SheezyArtAccounts.ToListAsync(cancellationToken);
            context.RemoveRange(accounts);

            await context.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(Setup));
        }
    }
}
