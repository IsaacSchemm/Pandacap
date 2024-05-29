using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Pandacap.Data;
using Pandacap.LowLevel;
using System.Net;
using System.Text;

namespace Pandacap.Controllers
{
    [Route("Posts")]
    public class PostsController(
        PandacapDbContext context,
        ActivityPubTranslator translator) : Controller
    {
        private static readonly IEnumerable<MediaTypeHeaderValue> ActivityJson = [
            MediaTypeHeaderValue.Parse("application/activity+json"),
            MediaTypeHeaderValue.Parse("application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\""),
            MediaTypeHeaderValue.Parse("application/json"),
            MediaTypeHeaderValue.Parse("text/json")
        ];

        private static readonly IEnumerable<MediaTypeHeaderValue> HTML = [
            MediaTypeHeaderValue.Parse("text/html")
        ];

        [Route("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            DeviantArtOurPost? post = null;
            post ??= await context.DeviantArtOurArtworkPosts.Where(p => p.Id == id).SingleOrDefaultAsync();
            post ??= await context.DeviantArtOurTextPosts.Where(p => p.Id == id).SingleOrDefaultAsync();

            if (post == null)
                return NotFound();

            var acceptedTypes = Request.Headers.Accept
                .SelectMany(str => str?.Split(",") ?? [])
                .Select(value => MediaTypeHeaderValue.Parse(value))
                .OrderByDescending(x => x, MediaTypeHeaderValueComparer.QualityComparer);

            foreach (var acceptedType in acceptedTypes)
            {
                foreach (var responseType in ActivityJson)
                    if (responseType.IsSubsetOf(acceptedType))
                        return Content(
                            ActivityPubSerializer.SerializeWithContext(translator.AsObject(post)),
                            "application/activity+json",
                            Encoding.UTF8);

                foreach (var responseType in HTML)
                    if (responseType.IsSubsetOf(acceptedType))
                        return Redirect(post?.Url ?? "/");
                        //return View(post);
            }

            return StatusCode((int)HttpStatusCode.NotAcceptable);
        }
    }
}
