using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub.Static;
using Pandacap.Constants;
using Pandacap.Database;

namespace Pandacap.Controllers
{
    [Route("")]
    public class NodeInfoController(
        PandacapDbContext context) : Controller
    {
        [HttpGet]
        [Route(".well-known/nodeinfo")]
        public IActionResult NodeInfo()
        {
            return Json(new
            {
                links = new[]
                {
                    new
                    {
                        rel = "http://nodeinfo.diaspora.software/ns/schema/2.1",
                        href = $"https://{ActivityPubHostInformation.ApplicationHostname}/.well-known/nodeinfo/2.1"
                    }
                }
            });
        }

        [HttpGet]
        [Route(".well-known/nodeinfo/2.1")]
        public async Task<IActionResult> NodeInfo2_1()
        {
            var posts = await context.Posts
                .Where(post => post.Type != Post.PostType.Scraps)
                .CountAsync();

            var communityPosts = await context.AddressedPosts
                .Where(ap => ap.InReplyTo == null)
                .CountAsync();

            var replies = await context.AddressedPosts
                .Where(ap => ap.InReplyTo != null)
                .CountAsync();

            return Json(new
            {
                version = "2.1",
                software = new
                {
                    name = UserAgentInformation.ApplicationName,
                    version = UserAgentInformation.VersionNumber,
                    homepage = UserAgentInformation.WebsiteUrl
                },
                protocols = new[]
                {
                    "activitypub"
                },
                services = new
                {
                    inbound = new[]
                    {
                        "atom1.0",
                        "rss2.0"
                    },
                    outbound = new[]
                    {
                        "atom1.0",
                        "rss2.0"
                    }
                },
                openRegistrations = false,
                usage = new
                {
                    users = new
                    {
                        total = 1,
                        activeHalfyear = 1,
                        activeMonth = 1
                    },
                    localPosts = posts + communityPosts,
                    localComments = replies
                }
            });
        }
    }
}
