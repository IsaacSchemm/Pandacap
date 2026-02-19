using Microsoft.AspNetCore.Mvc;
using Pandacap.ActivityPub;
using Pandacap.ConfigurationObjects;

namespace Pandacap.Controllers
{
    [Route("")]
    public class WebFingerController(
        ApplicationInformation appInfo,
        HostInformation hostInformation) : Controller
    {
        [HttpGet]
        [Route(".well-known/webfinger")]
        public IActionResult WebFinger(string resource)
        {
            string handle = $"acct:{appInfo.Username}@{appInfo.ApplicationHostname}";

            if (resource == handle || resource == hostInformation.ActorId)
            {
                return Json(new
                {
                    subject = handle,
                    aliases = new[] { hostInformation.ActorId },
                    links = new[]
                    {
                        new
                        {
                            rel = "http://webfinger.net/rel/profile-page",
                            type = "text/html",
                            href = hostInformation.ActorId
                        },
                        new
                        {
                            rel = "self",
                            type = "application/activity+json",
                            href = hostInformation.ActorId
                        }
                    }
                });
            }

            return NotFound();
        }
    }
}
