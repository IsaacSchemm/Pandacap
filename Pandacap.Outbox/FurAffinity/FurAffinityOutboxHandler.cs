using Pandacap.Database;
using Pandacap.FurAffinity.Interfaces;
using Pandacap.Outbox.Interfaces;

namespace Pandacap.Outbox.FurAffinity
{
    internal class FurAffinityOutboxHandler(
        IFurAffinityClientFactory furAffinityClientFactory,
        IEnumerable<IFurAffinityCredentials> furAffinityCredentials,
        PandacapDbContext pandacapDbContext) : IOutboxDestination
    {
        public async Task<bool> PublishNextQueuedPostAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task SynchronizeOfflinePlatformCacheAsync(CancellationToken cancellationToken)
        {
            var credentials = furAffinityCredentials.FirstOrDefault();

            if (credentials == null)
                return;

            var client = furAffinityClientFactory.CreateClient(credentials, Pandacap.FurAffinity.Models.Domain.SFW);

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
