using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.FSharp.Collections;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.LowLevel.MyLinks;

namespace Pandacap.HighLevel
{
    internal class MyLinkService(
        ApplicationInformation appInfo,
        BlueskyProfileResolver blueskyProfileResolver,
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
            var profiles = await blueskyProfileResolver.GetAsync([
                .. await context.ATProtoCredentials
                    .Where(c => c.CrosspostTargetSince != null)
                    .Select(c => c.DID)
                    .ToListAsync(),
                $"{appInfo.Username}.{appInfo.HandleHostname}.ap.brid.gy"
            ]);

            foreach (var profile in profiles)
            {
                yield return new(
                    platformName: "Bluesky",
                    url: $"https://bsky.app/profile/{profile.Handle}",
                    linkText: $"@{profile.Handle}");
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
