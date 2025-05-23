using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.Clients;
using Pandacap.Models;
using System.Text;

namespace Pandacap.Controllers
{
    [Route("UserPosts")]
    public class UserPostsController(
        BlobServiceClient blobServiceClient,
        PandacapDbContext context,
        PostCreator postCreator) : Controller
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

            return View(new UserPostViewModel
            {
                Post = post,
                Replies = []
            });
        }

        [HttpGet]
        [Authorize]
        [Route("CreateArtworkFromUpload")]
        public async Task<IActionResult> CreateArtworkFromUpload(Guid id)
        {
            var upload = await context.Uploads
                .Where(i => i.Id == id)
                .SingleAsync();

            return View(new CreateArtworkFromUploadViewModel
            {
                AltText = upload.AltText,
                UploadId = upload.Id
            });
        }

        [HttpPost]
        [Authorize]
        [Route("CreateArtworkFromUpload")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateArtworkFromUpload(
            CreateArtworkFromUploadViewModel model,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var id = await postCreator.CreatePostAsync(model, cancellationToken);

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

            var id = await postCreator.CreatePostAsync(model, cancellationToken);

            return RedirectToAction(nameof(Index), new { id });
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

            var id = await postCreator.CreatePostAsync(model, cancellationToken);

            return RedirectToAction(nameof(Index), new { id });
        }

        [HttpGet]
        [Authorize]
        [Route("CreateStatusUpdateFromUpload")]
        public async Task<IActionResult> CreateStatusUpdateFromUpload(Guid id)
        {
            var upload = await context.Uploads
                .Where(i => i.Id == id)
                .SingleAsync();

            return View(new CreateStatusUpdateFromUploadViewModel
            {
                UploadId = upload.Id,
                AltText = upload.AltText
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        [Route("CreateStatusUpdateFromUpload")]
        public async Task<IActionResult> CreateStatusUpdateFromUpload(
            CreateStatusUpdateFromUploadViewModel model,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var id = await postCreator.CreatePostAsync(model, cancellationToken);

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
