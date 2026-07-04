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

        public async Task SynchronizeFoldersAsync(CancellationToken cancellationToken)
        {
            if (await userAwareWeasylClientFactory.CreateWeasylClientAsync(cancellationToken) is not IWeasylClient client)
                return;

            var oldFolders = await pandacapDbContext.KnownWeasylFolders.ToListAsync(cancellationToken);
            var newFolders = await client.GetFoldersAsync(cancellationToken).ToListAsync(cancellationToken);

            foreach (var oldFolder in oldFolders)
                if (!newFolders.Any(f => f.FolderId == oldFolder.FolderId))
                    pandacapDbContext.KnownWeasylFolders.Remove(oldFolder);

            foreach (var newFolder in newFolders)
                if (!oldFolders.Any(f => f.FolderId == newFolder.FolderId))
                    pandacapDbContext.KnownWeasylFolders.Add(new()
                    {
                        FolderId = newFolder.FolderId,
                        Name = newFolder.Name
                    });

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
