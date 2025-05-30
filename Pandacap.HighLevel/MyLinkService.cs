using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.FSharp.Collections;
using Pandacap.ActivityPub;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.LowLevel.MyLinks;

namespace Pandacap.HighLevel
{
    internal class MyLinkService(
        ApplicationInformation appInfo,
        BlueskyProfileResolver blueskyProfileResolver,
        PandacapDbContext context,
        Mapper mapper,
        IMemoryCache memoryCache) : IMyLinkService
    {
        private static readonly Guid _cacheKey = Guid.NewGuid();

        public async Task<FSharpList<MyLink>> GetLinksAsync(CancellationToken cancellationToken)
        {
            return await memoryCache.GetOrCreateAsync<FSharpList<MyLink>>(
                _cacheKey,
                async cacheEntry =>
                {
                    try
                    {
                        return [.. await EnumerateLinks().ToListAsync(cancellationToken)];
                    }
                    catch (Exception)
                    {
                        return [];
                    }
                }) ?? [];
        }

        private async IAsyncEnumerable<MyLink> EnumerateLinks()
        {
            yield return new(
                platformName: "ActivityPub",
                url: mapper.ActorId,
                linkText: $"@{appInfo.Username}@{appInfo.HandleHostname}");

            await foreach (var x in context.ATProtoCredentials)
            {
                if (x.CrosspostTargetSince == null)
                {
                    continue;
                }

                var profiles = await blueskyProfileResolver.GetAsync([x.DID]);
                var profile = profiles.DefaultIfEmpty(null).First();
                var username = profile?.Handle ?? x.DID;

                yield return new(
                    platformName: "Bluesky",
                    url: $"https://bsky.app/profile/{username}",
                    linkText: $"https://bsky.app/profile/{username}");
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
