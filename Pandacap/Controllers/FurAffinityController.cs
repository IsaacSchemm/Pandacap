using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;

namespace Pandacap.Controllers
{
    [Authorize]
    public class FurAffinityController(
        PandacapDbContext context) : Controller
    {
        public async Task<IActionResult> Setup()
        {
            var credentials = await context.FurAffinityCredentials
                .AsNoTracking()
                .FirstOrDefaultAsync();

            ViewBag.Username = credentials?.Username;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Connect(string a, string b)
        {
            int count = await context.FurAffinityCredentials.CountAsync();
            if (count > 0)
                return Conflict();

            if (a != null && b != null)
            {
                var credentials = new FurAffinityCredentials
                {
                    A = a,
                    B = b
                };

                credentials.Username = await FurAffinityFs.FurAffinity.WhoamiAsync(credentials);

                context.FurAffinityCredentials.Add(credentials);

                await context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Setup));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reset()
        {
            var accounts = await context.FurAffinityCredentials.ToListAsync();
            context.RemoveRange(accounts);

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Setup));
        }
    }
}
