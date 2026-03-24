using Microsoft.AspNetCore.Mvc;
using Pandacap.ActivityPub.Static;
using Pandacap.ConfigurationObjects;

namespace Pandacap.Controllers
{
    [Route("")]
    public class WebFingerController(
        ApplicationInformation appInfo) : Controller
    {
        [HttpGet]
        [Route(".well-known/webfinger")]
        public IActionResult WebFinger(string resource)
        {
            string handle = $"acct:{appInfo.Username}@{appInfo.ApplicationHostname}";

            if (resource == handle || resource == ActivityPubHostInformation.ActorId)
            {
                return Json(new
                {
                    subject = handle,
                    aliases = new[] { ActivityPubHostInformation.ActorId },
                    links = new[]
                    {
                        new
                        {
                            rel = "http://webfinger.net/rel/profile-page",
                            type = "text/html",
                            href = ActivityPubHostInformation.ActorId
                        },
                        new
                        {
                            rel = "self",
                            type = "application/activity+json",
                            href = ActivityPubHostInformation.ActorId
                        }
                    }
                });
            }

            return NotFound();
        }
    }
}
