﻿using Azure.Storage.Blobs;
using CommonMark;
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
        [Route("CreateStatusUpdate")]
        public IActionResult CreateStatusUpdate()
        {
            return View();
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

            async IAsyncEnumerable<PostImage> uploadImagesAsync()
            {
                if (model.File == null)
                    yield break;

                Guid blobId = Guid.NewGuid();

                using var stream = model.File.OpenReadStream();

                await blobServiceClient
                    .GetBlobContainerClient("blobs")
                    .UploadBlobAsync($"{blobId}", stream, cancellationToken);

                yield return new()
                {
                    Blob = new()
                    {
                        Id = blobId,
                        ContentType = model.File.ContentType
                    },
                    AltText = model.AltText
                };
            }

            var post = new Post
            {
                Body = CommonMarkConverter.Convert(model.MarkdownBody),
                Id = id,
                Images = await uploadImagesAsync().ToListAsync(cancellationToken),
                PublishedTime = DateTimeOffset.UtcNow,
                Tags = model.DistinctTags.ToList(),
                Type = PostType.StatusUpdate
            };

            context.Posts.Add(post);

            foreach (string inbox in await deliveryInboxCollector.GetDeliveryInboxesAsync(
                includeGhosted: true,
                cancellationToken))
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
                Body = CommonMarkConverter.Convert(model.MarkdownBody),
                Id = id,
                PublishedTime = DateTimeOffset.UtcNow,
                Tags = model.DistinctTags.ToList(),
                Title = model.Title,
                Type = PostType.JournalEntry
            };

            context.Posts.Add(post);

            foreach (string inbox in await deliveryInboxCollector.GetDeliveryInboxesAsync(
                includeGhosted: true,
                cancellationToken))
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
        public IActionResult CreateArtwork()
        {
            return View();
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
            Guid blobId = Guid.NewGuid();

            using var stream = model.File!.OpenReadStream();

            await blobServiceClient
                .GetBlobContainerClient("blobs")
                .UploadBlobAsync($"{blobId}", stream, cancellationToken);

            var post = new Post
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
                Tags = model.DistinctTags.ToList(),
                Title = model.Title,
                Type = PostType.Artwork
            };

            context.Posts.Add(post);

            foreach (string inbox in await deliveryInboxCollector.GetDeliveryInboxesAsync(
                includeGhosted: true,
                cancellationToken))
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
                includeGhosted: true,
                cancellationToken))
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
