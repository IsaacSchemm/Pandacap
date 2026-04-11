using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Pandacap.ATProto.Services.Interfaces;
using Pandacap.Database;
using Pandacap.PlatformLinks.Interfaces;
using Pandacap.PlatformLinks.LinkTemplates;
using System.Runtime.CompilerServices;

namespace Pandacap.PlatformLinks
{
    internal class PlatformLinkProvider(
        PandacapDbContext context,
        IDIDResolver didResolver,
        IMemoryCache memoryCache) : IPlatformLinkProvider
    {
        private const string KEY = "9d3b19b8-b641-4ea2-8f03-0edd775618d3";

        public async Task<IReadOnlyList<IPlatformLink>> GetProfileLinksAsync(
            CancellationToken cancellationToken = default)
        {
            var list = await GetLinkTemplatesAsync(cancellationToken);
            return list
                .Select(link => new ProfileLink(link))
                .ToList();
        }

        public async Task<IReadOnlyList<IPlatformLink>> GetPostLinksAsync(
            IPlatformLinkPostSource post,
            CancellationToken cancellationToken = default)
        {
            var list = await GetLinkTemplatesAsync(cancellationToken);
            return list
                .Select(link => new PostLink(link, post))
                .ToList();
        }

        public async Task<IReadOnlyList<string>> GetBlueskyStyleAppViewHostsAsync(
            CancellationToken cancellationToken)
        {
            var list = await GetLinkTemplatesAsync(cancellationToken);

            return [.. list
                .OfType<BlueskyStyleATProtoPlatformLinkTemplate>()
                .Select(link => link.Host)];
        }

        private async Task<IReadOnlyList<ILinkTemplate>> GetLinkTemplatesAsync(
            CancellationToken cancellationToken = default)
        =>
            await memoryCache.GetOrCreateAsync<IReadOnlyList<ILinkTemplate>>(
                KEY,
                async _ => await EnumerateLinkTemplatesAsync(cancellationToken).ToListAsync(cancellationToken),
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(1)
                }) ?? [];

        private async IAsyncEnumerable<ILinkTemplate> EnumerateLinkTemplatesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new FediverseLinkTemplate("Mastodon", "mastodon.png");
            yield return new FediverseLinkTemplate("Pixelfed", "pixelfed.png");
            yield return new FediverseLinkTemplate("wafrn", "wafrn.png");

            yield return new BrowserPubLinkTemplate("browser.pub");

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

                yield return new BlueskyStyleATProtoPlatformLinkTemplate("Bluesky", "bluesky.png", "bsky.app", did, handle);
                yield return new BlueskyStyleATProtoPlatformLinkTemplate("Blacksky", "blacksky.png", "blacksky.community", did, handle);
                yield return new BlueskyStyleATProtoPlatformLinkTemplate("Red Dwarf", "reddwarf.ico", "reddwarf.app", did, handle);
            }

            await foreach (var x in context.DeviantArtCredentials)
            {
                yield return new DeviantArtPlatformLinkTemplate(x.Username);
            }

            await foreach (var x in context.FurAffinityCredentials)
            {
                yield return new FurAffinityPlatformLinkTemplate(x.Username);
            }

            await foreach (var x in context.WeasylCredentials)
            {
                yield return new WeasylPlatformLinkTemplate(x.Login);
            }
        }

        private record ProfileLink(ILinkTemplate Template) : IPlatformLink
        {
            PlatformLinkCategory IPlatformLink.Category => Template.Category;
            string? IPlatformLink.IconFilename => Template.IconFilename;
            string? IPlatformLink.PlatformName => Template.PlatformName;
            string? IPlatformLink.Username => Template.Username;
            string? IPlatformLink.Url => Template.GetViewProfileUrl();
        }

        private record PostLink(ILinkTemplate Template, IPlatformLinkPostSource Post) : IPlatformLink
        {
            PlatformLinkCategory IPlatformLink.Category => Template.Category;
            string? IPlatformLink.IconFilename => Template.IconFilename;
            string? IPlatformLink.PlatformName => Template.PlatformName;
            string? IPlatformLink.Username => Template.Username;
            string? IPlatformLink.Url => Template.GetViewPostUrl(Post);
        }
    }
}
