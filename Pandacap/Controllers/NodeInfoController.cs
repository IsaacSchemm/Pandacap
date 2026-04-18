using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub.Static;
using Pandacap.Constants;
using Pandacap.Database;

namespace Pandacap.Controllers
{
    [Route("")]
    public class NodeInfoController(
        PandacapDbContext pandacapDbContext) : Controller
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "Readability issues")]
        public async Task<IActionResult> NodeInfo2_1(CancellationToken cancellationToken)
        {
            var posts = await pandacapDbContext.Posts
                .Where(post => post.Type != Post.PostType.Scraps)
                .CountAsync(cancellationToken);

            var communityPosts = await pandacapDbContext.AddressedPosts
                .Where(ap => ap.InReplyTo == null)
                .CountAsync(cancellationToken);

            var replies = await pandacapDbContext.AddressedPosts
                .Where(ap => ap.InReplyTo != null)
                .CountAsync(cancellationToken);

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
