using Microsoft.AspNetCore.Mvc;
using Pandacap.ConfigurationObjects;

namespace Pandacap.Controllers
{
    [Route("")]
    public class WebFingerController(
        ApplicationInformation appInfo,
        ActivityPub.Mapper mapper) : Controller
    {
        [HttpGet]
        [Route(".well-known/webfinger")]
        public IActionResult WebFinger(string resource)
        {
            string handle = $"acct:{appInfo.Username}@{appInfo.ApplicationHostname}";

            if (resource == handle || resource == mapper.ActorId)
            {
                return Json(new
                {
                    subject = handle,
                    aliases = new[] { mapper.ActorId },
                    links = new[]
                    {
                        new
                        {
                            rel = "http://webfinger.net/rel/profile-page",
                            type = "text/html",
                            href = mapper.ActorId
                        },
                        new
                        {
                            rel = "self",
                            type = "application/activity+json",
                            href = mapper.ActorId
                        }
                    }
                });
            }

            return NotFound();
        }
    }
}
