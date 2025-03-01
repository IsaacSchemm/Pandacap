using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub;
using Pandacap.Clients;
using Pandacap.Data;
using Pandacap.Html;

namespace Pandacap.Controllers
{
    public class FurryNetworkController(
        PandacapDbContext context,
        FurryNetworkClient furryNetworkClient,
        Mapper mapper) : Controller
    {
        public async Task<IActionResult> Setup(CancellationToken cancellationToken)
        {
            var account = await context.FurryNetworkAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            ViewBag.Username = account?.Username;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(string username, CancellationToken cancellationToken)
        {
            var profile = await furryNetworkClient.GetProfileAsync(username);

            IEnumerable<Uri> findUris()
            {
                foreach (var cf in profile.customFields)
                    if (Uri.TryCreate(cf.value, UriKind.Absolute, out Uri? uri))
                        yield return uri;
            }

            if (!Uri.TryCreate(mapper.ActorId, UriKind.Absolute, out Uri? actorId)
                || !findUris().Contains(actorId))
            {
                return Content($"You must add a profile field to Furry Network with the Pandacap actor ID ({actorId}) as the value.");
            }

            var accounts = await context.FurryNetworkAccounts.ToListAsync(cancellationToken);
            context.RemoveRange(accounts);

            context.FurryNetworkAccounts.Add(new()
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
            var accounts = await context.FurryNetworkAccounts.ToListAsync(cancellationToken);
            context.RemoveRange(accounts);

            await context.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(Setup));
        }
    }
}
