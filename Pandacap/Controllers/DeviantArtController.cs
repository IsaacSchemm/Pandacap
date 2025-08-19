using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DeviantArtFs.Extensions;
using DeviantArtFs.ParameterTypes;
using DeviantArtFs.ResponseTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.HighLevel.DeviantArt;
using Pandacap.Clients;
using Pandacap.Models;
using Pandacap.PlatformBadges;
using Stash = DeviantArtFs.Api.Stash;
using Pandacap.Html;

namespace Pandacap.Controllers
{
    [Authorize]
    public class DeviantArtController(
        ApplicationInformation appInfo,
        BlobServiceClient blobServiceClient,
        PandacapDbContext context,
        DeviantArtCredentialProvider deviantArtCredentialProvider) : Controller
    {
        private record ThumbnailWrapper(Preview Item) : IPostThumbnail
        {
            string IPostThumbnail.Url => Item.src;
            string IPostThumbnail.AltText => "";
        }

        private record PostWrapper(Deviation Item) : IPost
        {
            PostPlatform IPost.Platform => PostPlatform.DeviantArt;
            string IPost.Url => Item.url.OrNull();
            string IPost.DisplayTitle => Item.title.OrNull() ?? $"{Item.deviationid}";
            string IPost.Id => $"{Item.deviationid}";
            string? IPost.InternalUrl => Item.url.OrNull();
            string? IPost.ExternalUrl => Item.url.OrNull();
            DateTimeOffset IPost.PostedAt => Item.published_time.OrNull() ?? DateTimeOffset.MinValue;
            string? IPost.ProfileUrl => null;
            IEnumerable<IPostThumbnail> IPost.Thumbnails => Item.thumbs.OrEmpty()
                .OrderByDescending(t => t.width * t.height)
                .Take(1)
                .Select(t => new ThumbnailWrapper(t));
            string? IPost.Username => null;
            string? IPost.Usericon => null;
        }

        public async Task<IActionResult> HomeFeed(
            int? next = null)
        {
            if (await deviantArtCredentialProvider.GetCredentialsAsync() is not (var token, _))
                return Content("No DeviantArt account is connected.");

            var page = await DeviantArtFs.Api.Browse.PageHomeAsync(
                token,
                PagingLimit.NewPagingLimit(24),
                PagingOffset.NewPagingOffset(next ?? 20));

            return View(
                "List",
                new ListViewModel
                {
                    Title = "DeviantArt Home Feed",
                    Items = new ListPage(
                        Current: [.. page.results.OrEmpty().Select(d => new PostWrapper(d))],
                        Next: page.next_offset.OrNull()?.ToString())
                });
        }

        private record FormFile(PostBlobRef BlobRef, BlobDownloadResult DownloadResult) : Stash.IFormFile
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
            var post = await context.Posts.FindAsync([id], cancellationToken);
            if (post == null)
                return NotFound();

            if (await deviantArtCredentialProvider.GetCredentialsAsync() is not (var token, var user))
                throw new Exception("No DeviantArt account connected");

            switch (post.Type)
            {
                case PostType.StatusUpdate:
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
                    await context.SaveChangesAsync(cancellationToken);

                    return RedirectToAction("Index", "UserPosts", new { id });
                case PostType.JournalEntry:
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
                    await context.SaveChangesAsync(cancellationToken);

                    return RedirectToAction("Index", "UserPosts", new { id });
                case PostType.Artwork:
                case PostType.Scraps:
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
            var post = await context.Posts.FindAsync([id], cancellationToken);
            if (post == null)
                return NotFound();

            if (post.IsTextPost)
                throw new Exception("Not an artwork post");

            if (await deviantArtCredentialProvider.GetCredentialsAsync() is not (var token, var user))
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
            var post = await context.Posts.FindAsync([id], cancellationToken);
            if (post == null)
                return NotFound();

            if (post.IsTextPost)
                throw new Exception("Not an artwork post");

            if (await deviantArtCredentialProvider.GetCredentialsAsync() is not (var token, var user))
                throw new Exception("No DeviantArt account connected");

            if (post.Images.Count != 1)
                throw new NotImplementedException($"Cannot crosspost artwork posts wih more or less than 1 image to DeviantArt");

            var blob = post.Images.Single().Raster;

            var blobResponse = await blobServiceClient
                .GetBlobContainerClient("blobs")
                .GetBlobClient($"{blob.Id}")
                .DownloadContentAsync(cancellationToken);

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
                    Stash.OriginalUrl.NewOriginalUrl($"https://{appInfo.ApplicationHostname}/UserPosts/{post.Id}"),
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
            await context.SaveChangesAsync(cancellationToken);

            return RedirectToAction("Index", "UserPosts", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Detach(Guid id)
        {
            var post = await context.Posts
                .Where(p => p.Id == id)
                .SingleAsync();

            post.DeviantArtId = null;
            post.DeviantArtUrl = null;

            await context.SaveChangesAsync();

            return RedirectToAction("Index", "UserPosts", new { id });
        }
    }
}
