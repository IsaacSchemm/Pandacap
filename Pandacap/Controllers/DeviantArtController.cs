using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.Models;
using Pandacap.DeviantArt.Interfaces;
using Pandacap.DeviantArt.Feeds.Interfaces;

namespace Pandacap.Controllers
{
    [Authorize]
    public class DeviantArtController(
        BlobServiceClient blobServiceClient,
        IDeviantArtClient deviantArtClient,
        IDeviantArtFeedProvider deviantArtFeedProvider,
        PandacapDbContext pandacapDbContext) : Controller
    {
        public async Task<IActionResult> HomeFeed(CancellationToken cancellationToken)
        {
            var items = await deviantArtFeedProvider.GetHomeFeedAsync()
                .Take(100)
                .ToListAsync(cancellationToken);

            return View(
                "List",
                new ListViewModel
                {
                    Title = "DeviantArt Home Feed",
                    Items = [.. items]
                });
        }

        private record FormFile(Post.Image.BlobRef BlobRef, BlobDownloadResult DownloadResult) : IArtworkFile
        {
            string IArtworkFile.ContentType => BlobRef.ContentType;
            byte[] IArtworkFile.Data => DownloadResult.Content.ToArray();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crosspost(
            Guid id,
            CancellationToken cancellationToken)
        {
            var post = await pandacapDbContext.Posts.FindAsync([id], cancellationToken);
            if (post == null)
                return NotFound();

            switch (post.Type)
            {
                case Post.PostType.StatusUpdate:
                    if (post.Images.Count > 0)
                        throw new NotImplementedException("Cannot crosspost a status update with an image to DeviantArt");

                    var dev = await deviantArtClient.PostStatusAsync(
                        post.Body,
                        cancellationToken);

                    post.DeviantArtId = dev.DeviationId;
                    post.DeviantArtUrl = dev.Url;
                    await pandacapDbContext.SaveChangesAsync(cancellationToken);

                    return RedirectToAction("Index", "UserPosts", new { id });

                case Post.PostType.JournalEntry:
                    var journal = await deviantArtClient.PostJournalAsync(
                        post.Title,
                        post.Body,
                        post.Tags,
                        cancellationToken);

                    post.DeviantArtId = journal.DeviationId;
                    post.DeviantArtUrl = journal.Url;
                    await pandacapDbContext.SaveChangesAsync(cancellationToken);

                    return RedirectToAction("Index", "UserPosts", new { id });

                case Post.PostType.Artwork:
                case Post.PostType.Scraps:
                    return RedirectToAction(nameof(CrosspostArtwork), new { id });

                default:
                    throw new NotImplementedException($"Cannot crosspost {post.Type} posts to DeviantArt");
            }
        }

        [HttpGet]
        public async Task<IActionResult> CrosspostArtwork(
            Guid id,
            CancellationToken cancellationToken)
        {
            var post = await pandacapDbContext.Posts.FindAsync([id], cancellationToken);
            if (post == null)
                return NotFound();

            if (post.IsTextPost)
                throw new Exception("Not an artwork post");

            var folders = await deviantArtClient
                .GetGalleryFoldersAsync()
                .Select(folder => new SelectListItem(folder.Name, $"{folder.FolderId}"))
                .ToListAsync(cancellationToken);

            return View(new DeviantArtCrosspostArtworkViewModel
            {
                Id = id,
                AvailableGalleryFolders = folders
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrosspostArtwork(
            Guid id,
            DeviantArtCrosspostArtworkViewModel model,
            CancellationToken cancellationToken)
        {
            var post = await pandacapDbContext.Posts.FindAsync([id], cancellationToken);
            if (post == null)
                return NotFound();

            if (post.IsTextPost)
                throw new Exception("Not an artwork post");

            if (post.Images.Count != 1)
                throw new NotImplementedException($"Cannot crosspost artwork posts wih more or less than 1 image to DeviantArt");

            var blob = post.Images.Single().Raster;

            var blobResponse = await blobServiceClient
                .GetBlobContainerClient("blobs")
                .GetBlobClient($"{blob.Id}")
                .DownloadContentAsync(cancellationToken);

            var response = await deviantArtClient.PostArtworkAsync(
                new FormFile(
                    blob,
                    blobResponse.Value),
                post.Title,
                post.Body,
                post.Tags,
                model.GalleryFolders,
                model.IsAiGenerated,
                model.NoAi,
                cancellationToken);

            post.DeviantArtId = response.DeviationId;
            post.DeviantArtUrl = response.Url;
            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction("Index", "UserPosts", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Detach(Guid id)
        {
            var post = await pandacapDbContext.Posts
                .Where(p => p.Id == id)
                .SingleAsync();

            post.DeviantArtId = null;
            post.DeviantArtUrl = null;

            await pandacapDbContext.SaveChangesAsync();

            return RedirectToAction("Index", "UserPosts", new { id });
        }
    }
}
