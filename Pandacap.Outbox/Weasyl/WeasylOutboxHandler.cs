using Microsoft.EntityFrameworkCore;
using Pandacap.Credentials.Interfaces;
using Pandacap.Database;
using Pandacap.Outbox.Interfaces;
using Pandacap.Text;
using Pandacap.Weasyl.Interfaces;

namespace Pandacap.Outbox.Weasyl
{
    internal class WeasylOutboxHandler(
        PandacapDbContext pandacapDbContext,
        IWeasylClientFactory weasylClientFactory) : IOutboxDestination
    {
        public async Task<bool> PublishNextQueuedPostAsync(CancellationToken cancellationToken)
        {
            if (weasylClientFactory.CreateWeasylClient() is not IWeasylClient client)
                return false;

            var self = await client.WhoamiAsync(cancellationToken);

            var post = await pandacapDbContext.Posts
                .Where(x => x.QueuedWeasylPost != null)
                .OrderBy(x => x.PublishedTime)
                .FirstOrDefaultAsync(cancellationToken);

            if (post == null)
                return false;

            var queued = post.QueuedWeasylPost!;

            post.WeasylUsername = self.login;
            post.WeasylSubmitId = null;
            post.WeasylJournalId = null;

            if (post.Images.FirstOrDefault() is Post.Image image)
            {
                post.WeasylSubmitId = await client.UploadVisualAsync(
                    post.GetImageUrl(image),
                    post.Title,
                    queued.Subtype,
                    queued.FolderId,
                    queued.Rating,
                    post.Body,
                    post.Tags,
                    cancellationToken);
            }
            else
            {
                post.WeasylJournalId = await client.UploadJournalAsync(
                    post.Title ?? ExcerptGenerator.FromText(40, post.Body),
                    queued.Rating,
                    post.Body,
                    post.Tags,
                    cancellationToken);
            }

            post.QueuedWeasylPost = null;

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task SynchronizeOfflinePlatformCacheAsync(CancellationToken cancellationToken)
        {
            if (weasylClientFactory.CreateWeasylClient() is not IWeasylClient client)
                return;

            var folders = await client.GetFoldersAsync(cancellationToken).ToListAsync(cancellationToken);

            await pandacapDbContext.OfflinePlatformDataCache.UpdateAsync(
                OfflinePlatformDataCacheItem.CachedPlatformDataType.WeasylGalleryFolders,
                folders,
                cancellationToken);
        }
    }
}
