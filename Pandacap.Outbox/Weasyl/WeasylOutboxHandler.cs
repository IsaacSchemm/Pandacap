using Microsoft.EntityFrameworkCore;
using Pandacap.Credentials.Interfaces;
using Pandacap.Database;
using Pandacap.Outbox.Interfaces;
using Pandacap.Weasyl.Interfaces;

namespace Pandacap.Outbox.Weasyl
{
    internal class WeasylOutboxHandler(
        PandacapDbContext pandacapDbContext,
        IUserAwareWeasylClientFactory userAwareWeasylClientFactory) : IOutboxDestination
    {
        public async Task<bool> PublishNextQueuedPostAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task SynchronizeOfflinePlatformCacheAsync(CancellationToken cancellationToken)
        {
            if (await userAwareWeasylClientFactory.CreateWeasylClientAsync(cancellationToken) is not IWeasylClient client)
                return;

            var folders = await client.GetFoldersAsync(cancellationToken).ToListAsync(cancellationToken);

            await pandacapDbContext.OfflinePlatformDataCache.UpdateAsync(
                OfflinePlatformDataCacheItem.CachedPlatformDataType.WeasylGalleryFolders,
                folders,
                cancellationToken);
        }
    }
}
