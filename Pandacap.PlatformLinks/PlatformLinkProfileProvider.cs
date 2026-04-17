using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.FSharp.Collections;
using Pandacap.ActivityPub.Static;
using Pandacap.ATProto.Services.Interfaces;
using Pandacap.Database;
using Pandacap.PlatformLinks.Interfaces;

namespace Pandacap.PlatformLinks
{
    public class PlatformLinkProfileProvider(
        IDIDResolver didResolver,
        IMemoryCache memoryCache,
        PandacapDbContext pandacapDbContext) : IPlatformLinkProfileProvider
    {
        private const string KEY = "ec9f7b3b-cd12-4ff5-bc63-274f01257c9c";

        private async Task<string?> TryGetBlueskyHandleAsync(
            CancellationToken cancellationToken)
        {
            var did = await pandacapDbContext.Posts
                .Where(post => post.BlueskyDID != null)
                .OrderByDescending(post => post.PublishedTime)
                .Select(post => post.BlueskyDID)
                .FirstOrDefaultAsync(cancellationToken);

            if (did == null)
                return null;

            string? handle = null;

            try
            {
                var doc = await didResolver.ResolveAsync(did, cancellationToken);
                handle = doc.Handle;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }

            return handle;
        }

        private async IAsyncEnumerable<string> GetDeviantArtUsernamesAsync()
        {
            await foreach (var credential in pandacapDbContext.DeviantArtCredentials)
                yield return credential.Username;
        }

        private async IAsyncEnumerable<string> GetFurAffinityUsernamesAsync()
        {
            await foreach (var credential in pandacapDbContext.FurAffinityCredentials)
                yield return credential.Username;
        }

        private async IAsyncEnumerable<string> GetWeasylUsernamesAsync()
        {
            await foreach (var credential in pandacapDbContext.WeasylCredentials)
                yield return credential.Login;
        }

        public async Task<IPlatformLinkProfile> GetProfileInformationAsync(CancellationToken cancellationToken) =>
            (await memoryCache.GetOrCreateAsync(
                KEY,
                async _ => new ProfileInformation(
                    BlueskyHandles: await TryGetBlueskyHandleAsync(cancellationToken) is string handle
                        ? [handle]
                        : [],
                    DeviantArtUsernames: [.. await GetDeviantArtUsernamesAsync().ToListAsync(cancellationToken)],
                    FurAffinityUsernames: [.. await GetFurAffinityUsernamesAsync().ToListAsync(cancellationToken)],
                    WeasylUsernames: [.. await GetWeasylUsernamesAsync().ToListAsync(cancellationToken)]),
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                }))!;

        public record ProfileInformation(
            FSharpList<string> BlueskyHandles,
            FSharpList<string> DeviantArtUsernames,
            FSharpList<string> FurAffinityUsernames,
            FSharpList<string> WeasylUsernames) : IPlatformLinkProfile
        {
            FSharpList<string> IPlatformLinkProfile.ActivityPubWebFingerHandles =>
                [$"@{ActivityPubHostInformation.Username}@{ActivityPubHostInformation.ApplicationHostname}"];
        }
    }
}
