using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;

namespace Pandacap.Controllers
{
    [Authorize]
    public class WeasylController(
        PandacapDbContext context,
        WeasylClientFactory weasylClientFactory) : Controller
    {
        public async Task<IActionResult> Setup()
        {
            var account = await context.WeasylCredentials
                .AsNoTracking()
                .Select(account => new
                {
                    account.Login,
                    account.Crosspost
                })
                .FirstOrDefaultAsync();

            ViewBag.Username = account?.Login;

            if (account != null)
            {
                if (await weasylClientFactory.CreateWeasylClientAsync() is WeasylClient client)
                {
                    var avatarResponse = await client.GetAvatarAsync(account.Login);
                    ViewBag.Avatar = avatarResponse.avatar;
                }

                ViewBag.Crosspost = account.Crosspost;
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Connect(string apiKey)
        {
            int count = await context.WeasylCredentials.CountAsync();
            if (count > 0)
                return Conflict();

            if (apiKey != null)
            {
                var weasylClient = weasylClientFactory.CreateWeasylClient(apiKey);

                var user = await weasylClient.WhoamiAsync();

                context.WeasylCredentials.Add(new WeasylCredentials
                {
                    Login = user.login,
                    ApiKey = apiKey
                });
                await context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Setup));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(bool? crosspost)
        {
            await foreach (var credentials in context.WeasylCredentials)
            {
                credentials.Crosspost = crosspost == true;
                await context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Setup));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reset()
        {
            var accounts = await context.WeasylCredentials.ToListAsync();
            context.RemoveRange(accounts);

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Setup));
        }
    }
}
