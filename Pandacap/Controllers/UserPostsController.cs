using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.LowLevel;
using Pandacap.Models;
using System.Text;

namespace Pandacap.Controllers
{
    [Route("UserPosts")]
    public class UserPostsController(
        BlobServiceClient blobServiceClient,
        PandacapDbContext context,
        DeliveryInboxCollector deliveryInboxCollector,
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

            return View(new UserPostViewModel
            {
                Post = post,
                Replies = User.Identity?.IsAuthenticated == true
                    ? await replyLookup
                        .CollectRepliesAsync(
                            mapper.GetObjectId(post),
                            cancellationToken)
                        .ToListAsync(cancellationToken)
                    : []
            });
        }

        [HttpGet]
        [Authorize]
        [Route("CreateStatusUpdate")]
        public async Task<IActionResult> CreateStatusUpdate(Guid? photoBinImageId = null)
        {
            var image = await context.PhotoBinImages
                .Where(i => i.Id == photoBinImageId)
                .SingleOrDefaultAsync();

            return View(new CreateStatusUpdateViewModel
            {
                PhotoBinImageId = image?.Id,
                AltText = image?.AltText
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        [Route("CreateStatusUpdate")]
        public async Task<IActionResult> CreateStatusUpdate(
            CreateStatusUpdateViewModel model,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Guid id = Guid.NewGuid();

            var post = new Post
            {
                Body = model.MarkdownBody,
                Id = id,
                Images = [],
                PublishedTime = DateTimeOffset.UtcNow,
                Tags = model.DistinctTags.ToList(),
                Type = PostType.StatusUpdate
            };

            if (model.PhotoBinImageId is Guid photoBinImageId)
            {
                var image = await context.PhotoBinImages
                    .Where(i => i.Id == photoBinImageId)
                    .SingleAsync(cancellationToken);

                context.Remove(image);

                post.Images = [new()
                {
                    Blob = new()
                    {
                        Id = image.Id,
                        ContentType = image.ContentType
                    },
                    AltText = model.AltText
                }];
            }

            context.Posts.Add(post);

            foreach (string inbox in await deliveryInboxCollector.GetDeliveryInboxesAsync(
                isCreate: true,
                cancellationToken: cancellationToken))
            {
                context.ActivityPubOutboundActivities.Add(new()
                {
                    Id = Guid.NewGuid(),
                    JsonBody = ActivityPubSerializer.SerializeWithContext(
                        translator.ObjectToCreate(
                            post)),
                    Inbox = inbox,
                    StoredAt = DateTimeOffset.UtcNow
                });
            }

            await context.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(Index), new { id });
        }

        [HttpGet]
        [Authorize]
        [Route("CreateJournalEntry")]
        public IActionResult CreateJournalEntry()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        [Route("CreateJournalEntry")]
        public async Task<IActionResult> CreateJournalEntry(
            CreateJournalEntryViewModel model,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Guid id = Guid.NewGuid();

            var post = new Post
            {
                Body = model.MarkdownBody,
                Id = id,
                PublishedTime = DateTimeOffset.UtcNow,
                Tags = model.DistinctTags.ToList(),
                Title = model.Title,
                Type = PostType.JournalEntry
            };

            context.Posts.Add(post);

            foreach (string inbox in await deliveryInboxCollector.GetDeliveryInboxesAsync(
                isCreate: true,
                cancellationToken: cancellationToken))
            {
                context.ActivityPubOutboundActivities.Add(new()
                {
                    Id = Guid.NewGuid(),
                    JsonBody = ActivityPubSerializer.SerializeWithContext(
                        translator.ObjectToCreate(
                            post)),
                    Inbox = inbox,
                    StoredAt = DateTimeOffset.UtcNow
                });
            }

            await context.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(Index), new { id });
        }

        [HttpGet]
        [Authorize]
        [Route("CreateArtwork")]
        public async Task<IActionResult> CreateArtwork(Guid photoBinImageId)
        {
            var image = await context.PhotoBinImages
                .Where(i => i.Id == photoBinImageId)
                .SingleAsync();

            return View(new CreateArtworkViewModel
            {
                AltText = image.AltText,
                PhotoBinImageId = image.Id
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        [Route("CreateArtwork")]
        public async Task<IActionResult> CreateArtwork(
            CreateArtworkViewModel model,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Guid id = Guid.NewGuid();

            var image = await context.PhotoBinImages
                .Where(i => i.Id == model.PhotoBinImageId)
                .SingleAsync(cancellationToken);

            context.Remove(image);

            var post = new Post
            {
                Body = model.MarkdownBody,
                Id = id,
                Images = [new()
                {
                    Blob = new()
                    {
                        Id = image.Id,
                        ContentType = image.ContentType
                    },
                    AltText = model.AltText,
                    FocalPoint = new()
                    {
                        Horizontal = 0,
                        Vertical = model.FocusTop ? 1 : 0
                    }
                }],
                PublishedTime = DateTimeOffset.UtcNow,
                Tags = model.DistinctTags.ToList(),
                Title = model.Title,
                Type = PostType.Artwork
            };

            context.Posts.Add(post);

            foreach (string inbox in await deliveryInboxCollector.GetDeliveryInboxesAsync(
                isCreate: true,
                cancellationToken: cancellationToken))
            {
                context.ActivityPubOutboundActivities.Add(new()
                {
                    Id = Guid.NewGuid(),
                    JsonBody = ActivityPubSerializer.SerializeWithContext(
                        translator.ObjectToCreate(
                            post)),
                    Inbox = inbox,
                    StoredAt = DateTimeOffset.UtcNow
                });
            }

            await context.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(Index), new { id });
        }

        [HttpPost]
        [Authorize]
        [Route("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            Guid id,
            CancellationToken cancellationToken)
        {
            var post = await context.Posts
                .Where(p => p.Id == id)
                .SingleAsync(cancellationToken);

            foreach (string inbox in await deliveryInboxCollector.GetDeliveryInboxesAsync(
                cancellationToken: cancellationToken))
            {
                context.ActivityPubOutboundActivities.Add(new()
                {
                    Id = Guid.NewGuid(),
                    JsonBody = ActivityPubSerializer.SerializeWithContext(
                        translator.ObjectToDelete(
                            post)),
                    Inbox = inbox,
                    StoredAt = DateTimeOffset.UtcNow
                });
            }

            context.Posts.Remove(post);

            await context.SaveChangesAsync(cancellationToken);

            foreach (var blob in post.Blobs)
            {
                await blobServiceClient
                    .GetBlobContainerClient("blobs")
                    .DeleteBlobIfExistsAsync($"{blob.Id}", cancellationToken: cancellationToken);
            }

            return RedirectToAction("Index", "Profile");
        }
    }
}
