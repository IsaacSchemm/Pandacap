using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.Models;
using Pandacap.UI.Elements;

namespace Pandacap.Controllers
{
    public class ExternalCredentialsController(
        PandacapDbContext pandacapDbContext) : Controller
    {
        private async Task<IReadOnlyList<IExternalCredentials>> GetExternalCredentialsAsync(
            CancellationToken cancellationToken
        ) => [
            .. await pandacapDbContext.DeviantArtCredentials.ToListAsync(cancellationToken),
            .. await pandacapDbContext.FurAffinityCredentials.ToListAsync(cancellationToken),
            .. await pandacapDbContext.WeasylCredentials.ToListAsync(cancellationToken),
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
            string platformName,
            CancellationToken cancellationToken)
        {
            var credentials = await GetExternalCredentialsAsync(cancellationToken);

            foreach (var credential in credentials)
                if (credential.Username == username && credential.PlatformName == platformName)
                    pandacapDbContext.Remove(credential);

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(Index));
        }
    }
}
