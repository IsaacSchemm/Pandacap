using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pandacap.Clients;
using Pandacap.ConfigurationObjects;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    [Authorize]
    public class MastodonController(
        IHttpClientFactory httpClientFactory) : Controller
    {
        public IActionResult StartViewingLocalTimeline(string host)
        {
            return RedirectToAction(nameof(ViewLocalTimeline), new { host });
        }

        [Route("Mastodon/ViewLocalTimeline/{host}")]
        public async Task<IActionResult> ViewLocalTimeline(
            string host,
            string? next = null,
            CancellationToken cancellationToken = default)
        {
            using var client = httpClientFactory.CreateClient();

            var posts = await Mastodon.GetLocalTimelineAsync(client, host, next, cancellationToken);

            return View(
                "List",
                new ListViewModel
                {
                    Title = $"Local feed for {host}",
                    Items = new ListPage(
                        Current: posts,
                        Next: posts.Select(p => p.Id).LastOrDefault())
                });
        }
    }
}
