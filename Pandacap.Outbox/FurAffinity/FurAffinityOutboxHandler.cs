using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.FurAffinity.Interfaces;
using Pandacap.Outbox.Interfaces;
using Pandacap.Text;

namespace Pandacap.Outbox.FurAffinity
{
    internal class FurAffinityOutboxHandler(
        IFurAffinityClientFactory furAffinityClientFactory,
        IEnumerable<IFurAffinityCredentials> furAffinityCredentials,
        IHttpClientFactory httpClientFactory,
        PandacapDbContext pandacapDbContext) : IOutboxDestination
    {
        private async Task<byte[]?> TryGetImageDataAsync(Post post)
        {
            foreach (var image in post.Images)
            {
                if (post.GetImageUrl(image) is string url)
                {
                    using var client = httpClientFactory.CreateClient();
                    using var resp = await client.GetAsync(url);
                    return await resp.EnsureSuccessStatusCode().Content.ReadAsByteArrayAsync();
                }
            }

            return null;
        }

        public async Task<bool> PublishNextQueuedPostAsync(CancellationToken cancellationToken)
        {
            var credentials = furAffinityCredentials.FirstOrDefault();

            if (credentials == null)
                return false;

            var post = await pandacapDbContext.Posts
                .Where(x => x.QueuedFurAffinityPost != null)
                .OrderBy(x => x.PublishedTime)
                .FirstOrDefaultAsync(cancellationToken);

            if (post == null)
                return false;

            var queued = post.QueuedFurAffinityPost!;

            var client = furAffinityClientFactory.CreateClient(credentials, Pandacap.FurAffinity.Models.Domain.WWW);

            post.FurAffinityUsername = await client.WhoamiAsync(cancellationToken);
            post.FurAffinitySubmissionId = null;
            post.FurAffinityJournalId = null;

            if (await TryGetImageDataAsync(post) is byte[] imageData)
            {
                Uri posted = await client.PostArtworkAsync(
                    imageData,
                    new Pandacap.FurAffinity.Models.ArtworkMetadata(
                        post.Title,
                        post.Body,
                        [.. post.Tags],
                        queued.Cat,
                        queued.Scrap,
                        queued.Atype,
                        queued.Species,
                        queued.Gender,
                        queued.Rating,
                        queued.LockComments,
                        [.. queued.FolderIds]),
                    cancellationToken);

                post.FurAffinitySubmissionId = int.Parse(posted.Segments[2].TrimEnd('/'));
            }
            else
            {
                Uri posted = await client.PostJournalAsync(
                    post.Title ?? ExcerptGenerator.FromText(40, post.Body),
                    post.Body,
                    queued.Rating,
                    cancellationToken);

                post.FurAffinityJournalId = int.Parse(posted.Segments[2].TrimEnd('/'));
            }

            post.QueuedFurAffinityPost = null;

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task SynchronizeOfflinePlatformCacheAsync(CancellationToken cancellationToken)
        {
            var credentials = furAffinityCredentials.FirstOrDefault();

            if (credentials == null)
                return;

            var client = furAffinityClientFactory.CreateClient(credentials, Pandacap.FurAffinity.Models.Domain.WWW);

            var folders = await client.ListGalleryFoldersAsync(cancellationToken);

            await pandacapDbContext.OfflinePlatformDataCache.UpdateAsync(
                OfflinePlatformDataCacheItem.CachedPlatformDataType.FurAffinityGalleryFolders,
                folders,
                cancellationToken);

            var postOptions = await client.ListPostOptionsAsync(cancellationToken);

            await pandacapDbContext.OfflinePlatformDataCache.UpdateAsync(
                OfflinePlatformDataCacheItem.CachedPlatformDataType.FurAffinityPostOptions,
                postOptions,
                cancellationToken);
        }
    }
}
