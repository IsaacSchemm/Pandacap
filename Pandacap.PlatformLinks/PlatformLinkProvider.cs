using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Pandacap.ATProto.Services.Interfaces;
using Pandacap.Database;
using Pandacap.PlatformLinks.Interfaces;
using System.Runtime.CompilerServices;

namespace Pandacap.PlatformLinks
{
    internal class PlatformLinkProvider(
        PandacapDbContext context,
        IDIDResolver didResolver,
        IMemoryCache memoryCache) : IPlatformLinkProvider
    {
        private const string KEY = "9d3b19b8-b641-4ea2-8f03-0edd775618d3";

        public async Task<IReadOnlyList<IPlatformLink>> GetPlatformLinksAsync(
            CancellationToken cancellationToken = default)
        =>
            await memoryCache.GetOrCreateAsync<IReadOnlyList<IPlatformLink>>(
                KEY,
                async _ => await GetUnderlyingPlatformLinksAsync(cancellationToken).ToListAsync(cancellationToken),
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(1)
                })
            ?? [];

        public async Task<IReadOnlyList<string>> GetBlueskyStyleAppViewHostsAsync(
            CancellationToken cancellationToken)
        =>
            await GetUnderlyingPlatformLinksAsync(cancellationToken)
                .OfType<BlueskyStyleATProtoPlatformLink>()
                .Select(link => link.Host)
                .ToListAsync(cancellationToken);

        private async IAsyncEnumerable<IPlatformLink> GetUnderlyingPlatformLinksAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new FediverseLink("mastodon.png", "Mastodon");
            yield return new FediverseLink("pixelfed.png", "Pixelfed");
            yield return new FediverseLink("wafrn.png", "wafrn");

            yield return new BrowserPubLink("browser.pub");

            var did = await context.Posts
                .OrderByDescending(post => post.PublishedTime)
                .Where(post => post.BlueskyDID != null)
                .Select(post => post.BlueskyDID)
                .FirstOrDefaultAsync(cancellationToken);

            if (did != null)
            {
                string? handle = null;

                try
                {
                    var doc = await didResolver.ResolveAsync(did, cancellationToken);
                    handle = doc?.Handle;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }

                yield return new BlueskyStyleATProtoPlatformLink("Bluesky", "bluesky.png", "bsky.app", did, handle);
                yield return new BlueskyStyleATProtoPlatformLink("Blacksky", "blacksky.png", "blacksky.community", did, handle);
                yield return new BlueskyStyleATProtoPlatformLink("Red Dwarf", "reddwarf.ico", "reddwarf.app", did, handle);
            }

            await foreach (var x in context.DeviantArtCredentials)
            {
                yield return new DeviantArtPlatformLink(x.Username);
            }

            await foreach (var x in context.FurAffinityCredentials)
            {
                yield return new FurAffinityPlatformLink(x.Username);
            }

            await foreach (var x in context.WeasylCredentials)
            {
                yield return new WeasylPlatformLink(x.Login);
            }
        }
    }
}
