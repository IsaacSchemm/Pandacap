using Azure.Storage.Blobs;
using CommonMark;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.LowLevel;
using Pandacap.Models;
using System.Net;
using System.Text;

namespace Pandacap.Controllers
{
    [Route("UserPosts")]
    public class UserPostsController(
        BlobServiceClient blobServiceClient,
        PandacapDbContext context,
        IdMapper mapper,
        ReplyLookup replyLookup,
        ActivityPubTranslator translator) : Controller
    {
        [Route("{id}")]
        public async Task<IActionResult> Index(
            Guid id,
            CancellationToken cancellationToken)
        {
            var post = await context.Posts
                .Where(p => p.Id == id)
                .SingleOrDefaultAsync(cancellationToken);

            if (post == null)
                return NotFound();

            if (Request.IsActivityPub())
                return Content(
                    ActivityPubSerializer.SerializeWithContext(translator.AsObject(post)),
                    "application/activity+json",
                    Encoding.UTF8);

            bool loggedIn = User.Identity?.IsAuthenticated == true;

            return View(new UserPostViewModel
            {
                Post = post,
                Replies = await replyLookup
                    .CollectRepliesAsync(
                        mapper.GetObjectId(post),
                        loggedIn,
                        cancellationToken)
                    .ToListAsync(cancellationToken)
            });
        }

        [HttpGet]
        [Authorize]
        [Route("CreateArtwork")]
        public IActionResult CreateArtwork()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        [Route("CreateArtwork")]
        public async Task<IActionResult> CreateArtwork(CreateArtworkViewModel model)
        {
            if (model.File == null)
                return BadRequest("No file was provided for upload.");

            Guid id = Guid.NewGuid();
            Guid blobId = Guid.NewGuid();

            using var stream = model.File.OpenReadStream();

            await blobServiceClient
                .GetBlobContainerClient("blobs")
                .UploadBlobAsync($"{blobId}", stream);

            context.Posts.Add(new Post
            {
                Body = CommonMarkConverter.Convert(model.MarkdownBody),
                Id = id,
                Images = [new()
                {
                    Blob = new()
                    {
                        Id = blobId,
                        ContentType = model.File.ContentType
                    },
                    AltText = model.AltText
                }],
                PublishedTime = DateTimeOffset.UtcNow,
                Sensitive = model.Sensitive,
                Summary = model.Summary,
                Tags = model.Tags
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(tag => tag.TrimStart('#'))
                    .Distinct()
                    .ToList(),
                Title = model.Title,
                Type = PostType.Artwork
            });

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { id });
        }

        [HttpPost]
        [Authorize]
        [Route("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var post = await context.Posts.Where(p => p.Id == id).SingleAsync();

            async IAsyncEnumerable<string> getInboxesAsync()
            {
                var ghosted = await context.Follows
                    .Where(f => f.Ghost)
                    .Select(f => f.ActorId)
                    .ToListAsync();

                await foreach (var follower in context.Followers)
                {
                    yield return follower.SharedInbox ?? follower.Inbox;
                }
            }

            await foreach (string inbox in getInboxesAsync().Distinct())
            {
                Guid activityGuid = Guid.NewGuid();

                string activityJson = ActivityPubSerializer.SerializeWithContext(
                    translator.ObjectToDelete(post));

                context.ActivityPubOutboundActivities.Add(new()
                {
                    Id = activityGuid,
                    JsonBody = activityJson,
                    Inbox = inbox,
                    StoredAt = DateTimeOffset.UtcNow
                });
            }

            return RedirectToAction("Index", "Profile");
        }
    }
}
