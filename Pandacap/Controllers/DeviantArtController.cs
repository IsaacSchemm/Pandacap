using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DeviantArtFs.Extensions;
using DeviantArtFs.ParameterTypes;
using DeviantArtFs.ResponseTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.Credentials.Interfaces;
using Pandacap.Models;
using Pandacap.Text;
using Stash = DeviantArtFs.Api.Stash;
using Pandacap.DeviantArt.Interfaces;

namespace Pandacap.Controllers
{
    [Authorize]
    public class DeviantArtController(
        BlobServiceClient blobServiceClient,
        IDeviantArtCredentialProvider deviantArtCredentialProvider,
        IDeviantArtFeedProvider deviantArtFeedProvider,
        PandacapDbContext pandacapDbContext) : Controller
    {
        public async Task<IActionResult> HomeFeed(CancellationToken cancellationToken)
        {
            var token = await deviantArtCredentialProvider.GetTokenAsync();
            if (token == null)
                return Content("No DeviantArt account is connected.");

            var items = await deviantArtFeedProvider.GetHomeFeedAsync(token)
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

        private record FormFile(Post.Image.BlobRef BlobRef, BlobDownloadResult DownloadResult) : Stash.IFormFile
        {
            string Stash.IFormFile.Filename => "file." + BlobRef.ContentType.Split('/').Last();
            string Stash.IFormFile.ContentType => BlobRef.ContentType;
            byte[] Stash.IFormFile.Data => DownloadResult.Content.ToArray();
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

            var token = await deviantArtCredentialProvider.GetTokenAsync();
            var user = await deviantArtCredentialProvider.GetUserAsync();
            if (token == null || user == null)
                throw new Exception("No DeviantArt account connected");

            switch (post.Type)
            {
                case Post.PostType.StatusUpdate:
                    if (post.Images.Count > 0)
                        throw new NotImplementedException("Cannot crosspost a status update with an image to DeviantArt");

                    var statusResponse = await DeviantArtFs.Api.User.PostStatusAsync(
                        token,
                        new DeviantArtFs.Api.User.EmbeddableStatusContent(
                            DeviantArtFs.Api.User.EmbeddableObject.Nothing,
                            DeviantArtFs.Api.User.EmbeddableObjectParent.NoParent,
                            DeviantArtFs.Api.User.EmbeddableStashItem.NoStashItem),
                        post.Body);

                    var dev = await DeviantArtFs.Api.Deviation.GetAsync(
                        token,
                        statusResponse.statusid);

                    post.DeviantArtId = dev.deviationid;
                    post.DeviantArtUrl = dev.url.Value;
                    await pandacapDbContext.SaveChangesAsync(cancellationToken);

                    return RedirectToAction("Index", "UserPosts", new { id });
                case Post.PostType.JournalEntry:
                    var journalResponse = await DeviantArtFs.Api.Deviation.Journal.CreateAsync(
                        token,
                        [
                            DeviantArtFs.Api.Deviation.Journal.ImmutableField.NewBody(post.Body)
                        ],
                        [
                            DeviantArtFs.Api.Deviation.Journal.MutableField.NewTitle(post.Title),
                            ..post.Tags.Select(tag => DeviantArtFs.Api.Deviation.Journal.MutableField.NewTag(tag)),
                            DeviantArtFs.Api.Deviation.Journal.MutableField.NewIsMature(false),
                            DeviantArtFs.Api.Deviation.Journal.MutableField.NewAllowComments(true)
                        ]);

                    var journal = await DeviantArtFs.Api.Deviation.GetAsync(
                        token,
                        journalResponse.deviationid);

                    post.DeviantArtId = journal.deviationid;
                    post.DeviantArtUrl = journal.url.Value;
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

            var token = await deviantArtCredentialProvider.GetTokenAsync();
            var user = await deviantArtCredentialProvider.GetUserAsync();
            if (token == null || user == null)
                throw new Exception("No DeviantArt account connected");

            var folders = await DeviantArtFs.Api.Gallery
                .GetFoldersAsync(
                    token,
                    CalculateSize.NewCalculateSize(false),
                    FolderPreload.NewFolderPreload(false),
                    FilterEmptyFolder.NewFilterEmptyFolder(false),
                    UserScope.ForCurrentUser,
                    PagingLimit.DefaultPagingLimit,
                    PagingOffset.StartingOffset)
                .Select(folder => new SelectListItem(folder.name, $"{folder.folderid}"))
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

            var token = await deviantArtCredentialProvider.GetTokenAsync();
            var user = await deviantArtCredentialProvider.GetUserAsync();
            if (token == null || user == null)
                throw new Exception("No DeviantArt account connected");

            if (post.Images.Count != 1)
                throw new NotImplementedException($"Cannot crosspost artwork posts wih more or less than 1 image to DeviantArt");

            var blob = post.Images.Single().Raster;

            var blobResponse = await blobServiceClient
                .GetBlobContainerClient("blobs")
                .GetBlobClient($"{blob.Id}")
                .DownloadContentAsync(cancellationToken);

            try
            {
                var stashResult = await Stash.SubmitAsync(
                    token,
                    Stash.SubmissionDestination.Default,
                    new Stash.SubmissionParameters(
                        Stash.SubmissionTitle.NewSubmissionTitle(
                            ExcerptGenerator.FromText(
                                50,
                                post.Title)),
                        Stash.ArtistComments.NewArtistComments(post.Body),
                        Stash.TagList.Create(post.Tags),
                        Stash.OriginalUrl.NoOriginalUrl,
                        is_dirty: false),
                    new FormFile(
                        blob,
                        blobResponse.Value));

                var response = await Stash.PublishAsync(
                    token,
                    [
                        ..model.GalleryFolders.Select(id => Stash.PublishParameter.NewGalleryId(id)),
                            Stash.PublishParameter.NewMaturity(Maturity.NotMature),
                            Stash.PublishParameter.NewAllowComments(true),
                            Stash.PublishParameter.NewAllowFreeDownload(true),
                            ..post.Tags.Select(tag => Stash.PublishParameter.NewTag(tag)),
                            model.IsAiGenerated
                                ? Stash.PublishParameter.IsAiGenerated
                                : Stash.PublishParameter.IsNotAiGenerated,
                            model.NoAi
                                ? Stash.PublishParameter.NoThirdPartyAi
                                : Stash.PublishParameter.ThirdPartyAiOk
                    ],
                    Stash.Item.NewItem(stashResult.itemid));

                if (response.status != "success")
                    throw new NotImplementedException($"DeviantArt response: {response.status}");

                post.DeviantArtId = response.deviationid;
                post.DeviantArtUrl = response.url;
                await pandacapDbContext.SaveChangesAsync(cancellationToken);

                return RedirectToAction("Index", "UserPosts", new { id });
            }
            catch (Exception ex) when (ex.Message.StartsWith("Invalid JSON"))
            {
                return Redirect("https://www.deviantart.com/stash");
            }
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
