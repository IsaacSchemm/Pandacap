using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.FSharp.Collections;
using Pandacap.Data;
using Pandacap.LowLevel.MyLinks;

namespace Pandacap.HighLevel
{
    internal class MyLinkService(
        PandacapDbContext context,
        IMemoryCache memoryCache) : IMyLinkService
    {
        private static readonly Guid _cacheKey = Guid.NewGuid();

        public async Task<FSharpList<MyLink>> GetLinksAsync(CancellationToken cancellationToken)
        {
            if (memoryCache.TryGetValue(_cacheKey, out var found) && found is FSharpList<MyLink> foundList)
                return foundList;

            try
            {
                return memoryCache.Set<FSharpList<MyLink>>(
                    _cacheKey,
                    [.. await EnumerateLinks().ToListAsync(cancellationToken)],
                    DateTimeOffset.UtcNow.AddMinutes(30));
            }
            catch (Exception)
            {
                return memoryCache.Set<FSharpList<MyLink>>(
                    _cacheKey,
                    [],
                    DateTimeOffset.UtcNow.AddMinutes(5));
            }
        }

        private async IAsyncEnumerable<MyLink> EnumerateLinks()
        {
            await foreach (var x in context.ATProtoCredentials)
            {
                if (x.CrosspostTargetSince == null)
                    continue;

                yield return new(
                    platformName: "Bluesky",
                    url: $"https://bsky.app/profile/{x.Handle ?? x.DID}",
                    linkText: x.Handle ?? x.DID);
            }

            await foreach (var x in context.DeviantArtCredentials)
            {
                yield return new(
                    platformName: "DeviantArt",
                    url: $"https://www.deviantart.com/{x.Username}",
                    linkText: x.Username);
            }

            await foreach (var x in context.FurAffinityCredentials)
            {
                yield return new(
                    platformName: "Fur Affinity",
                    url: $"https://www.furaffinity.net/user/{x.Username}",
                    linkText: x.Username);
            }

            await foreach (var x in context.WeasylCredentials)
            {
                yield return new(
                    platformName: "Weasyl",
                    url: $"https://www.weasyl.com/~{x.Login}",
                    linkText: x.Login);
            }
        }
    }
}
