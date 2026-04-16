using Microsoft.EntityFrameworkCore;
using Pandacap.ATProto.Services.Interfaces;
using Pandacap.Database;
using Pandacap.PlatformLinks.Interfaces;
using Pandacap.PlatformLinks.LinkTemplates;
using System.Runtime.CompilerServices;

namespace Pandacap.PlatformLinks
{
    internal class PlatformLinkProvider(
        PandacapDbContext context,
        IDIDResolver didResolver) : IPlatformLinkProvider
    {
        public async Task<IReadOnlyList<IPlatformLink>> GetProfileLinksAsync(
            CancellationToken cancellationToken = default)
        =>
            await EnumerateLinkTemplatesAsync()
            .Select(link => new ProfileLink(link))
            .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<IPlatformLink>> GetPostLinksAsync(
            IPlatformLinkPostSource post,
            CancellationToken cancellationToken = default)
        =>
            await EnumerateLinkTemplatesAsync()
            .Select(link => new PostLink(link, post))
            .Where(link => link.Url != null)
            .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<string>> GetBlueskyStyleAppViewHostsAsync(
            CancellationToken cancellationToken)
        =>
            await EnumerateBlueskyLinkTemplatesAsync(cancellationToken)
            .Select(link => link.Host)
            .ToListAsync(cancellationToken);

        private async IAsyncEnumerable<ILinkTemplate> EnumerateLinkTemplatesAsync()
        {
            yield return new FediverseLinkTemplate("ActivityPub");
            yield return new FediverseLinkTemplate("Mastodon", "mastodon.png");
            yield return new FediverseLinkTemplate("Pixelfed", "pixelfed.png");
            yield return new FediverseLinkTemplate("wafrn", "wafrn.png");

            yield return new BrowserPubLinkTemplate("browser.pub");

            await foreach (var x in EnumerateBlueskyLinkTemplatesAsync())
            {
                yield return x;
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

        private async IAsyncEnumerable<BlueskyStyleATProtoPlatformLinkTemplate> EnumerateBlueskyLinkTemplatesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
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
        }

        private record ProfileLink(ILinkTemplate Template) : IPlatformLink
        {
            public PlatformLinkCategory Category => Template.Category;
            public string? IconFilename => Template.IconFilename;
            public string? PlatformName => Template.PlatformName;
            public string? Username => Template.Username;
            public string? Url => Template.GetViewProfileUrl();
        }

        private record PostLink(ILinkTemplate Template, IPlatformLinkPostSource Post) : IPlatformLink
        {
            public PlatformLinkCategory Category => Template.Category;
            public string? IconFilename => Template.IconFilename;
            public string? PlatformName => Template.PlatformName;
            public string? Username => null;
            public string? Url => Template.GetViewPostUrl(Post);
        }
    }
}
