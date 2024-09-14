using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.LowLevel;
using System.Text;

namespace Pandacap.Controllers
{
    [Route("AddressedPosts")]
    public class AddressedPostsController(
        PandacapDbContext context,
        ActivityPubTranslator translator) : Controller
    {
        [Route("{id}")]
        public async Task<IActionResult> Index(Guid id)
        {
            var post = await context.AddressedPosts.Where(p => p.Id == id).SingleOrDefaultAsync();

            if (post == null)
                return NotFound();

            //if (Request.IsActivityPub())
                return Content(
                    ActivityPubSerializer.SerializeWithContext(translator.AsObject(post)),
                    "application/activity+json",
                    Encoding.UTF8);

            //return Content(post.HtmlContent, "text/html");
        }
    }
}
