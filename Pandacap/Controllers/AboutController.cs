using Microsoft.AspNetCore.Mvc;
using Pandacap.HighLevel;
using Pandacap.JsonLd;

namespace Pandacap.Controllers
{
    public class AboutController(ActivityPubRemoteActorService activityPubRemoteActorService) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var o = await activityPubRemoteActorService.FetchAddresseeAsync("https://threads.net/ap/users/17841459227751075/", CancellationToken.None);

            return View();
        }
    }
}
