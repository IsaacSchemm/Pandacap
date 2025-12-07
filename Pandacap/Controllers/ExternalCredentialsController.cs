using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    public class ExternalCredentialsController(
        PandacapDbContext context) : Controller
    {
        private async Task<IReadOnlyList<IExternalCredentials>> GetExternalCredentialsAsync(
            CancellationToken cancellationToken
        ) => [
            .. await context.DeviantArtCredentials.ToListAsync(cancellationToken),
            .. await context.FurAffinityCredentials.ToListAsync(cancellationToken),
            .. await context.RedditCredentials.ToListAsync(cancellationToken),
            .. await context.WeasylCredentials.ToListAsync(cancellationToken),
        ];

        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            return View(new ExternalCredentialsModel
            {
                ExternalCredentials = await GetExternalCredentialsAsync(cancellationToken)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(
            string username,
            string platform,
            CancellationToken cancellationToken)
        {
            var credentials = await GetExternalCredentialsAsync(cancellationToken);

            foreach (var credential in credentials)
                if (credential.Username == username && $"{credential.Platform}" == platform)
                    context.Remove(credential);

            await context.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(Index));
        }
    }
}
