using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.FurAffinity.Interfaces;
using Pandacap.Outbox.Interfaces;

namespace Pandacap.Outbox.FurAffinity
{
    internal class FurAffinityOutboxHandler(
        IFurAffinityClientFactory furAffinityClientFactory,
        PandacapDbContext pandacapDbContext) : IOutboxDestination
    {
        public async Task<bool> PublishNextQueuedPostAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task SynchronizeFoldersAsync(CancellationToken cancellationToken)
        {
            var credentials = await pandacapDbContext.FurAffinityCredentials.SingleOrDefaultAsync(cancellationToken);

            if (credentials == null)
                return;

            var client = furAffinityClientFactory.CreateClient(credentials, Pandacap.FurAffinity.Models.Domain.SFW);

            var oldFolders = await pandacapDbContext.KnownFurAffinityFolders.ToListAsync(cancellationToken);
            var newFolders = await client.ListGalleryFoldersAsync(cancellationToken);

            foreach (var oldFolder in oldFolders)
                if (!newFolders.Any(f => f.FolderId == oldFolder.FolderId))
                    pandacapDbContext.KnownFurAffinityFolders.Remove(oldFolder);

            foreach (var newFolder in newFolders)
                if (!oldFolders.Any(f => f.FolderId == newFolder.FolderId))
                    pandacapDbContext.KnownFurAffinityFolders.Add(new()
                    {
                        FolderId = newFolder.FolderId,
                        Name = newFolder.Name
                    });

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
