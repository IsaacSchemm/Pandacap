using Microsoft.EntityFrameworkCore;
using Pandacap.ATProto.Services.Interfaces;
using Pandacap.Database;
using Pandacap.PlatformLinks.Interfaces;
using Pandacap.PlatformLinks.LinkTemplates;
using Pandacap.PlatformLinks.Models;
using System.Runtime.CompilerServices;

namespace Pandacap.PlatformLinks
{
    internal class PlatformLinkProvider(
        PandacapDbContext context,
        IDIDResolver didResolver) : IPlatformLinkProvider
    {
        private async IAsyncEnumerable<BlueskyLinkTemplate> GetBlueskyAppViewsAsync(
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

                yield return new BlueskyLinkTemplate("Bluesky", "bluesky.png", "bsky.app", did, handle);
                yield return new BlueskyLinkTemplate("Blacksky", "blacksky.png", "blacksky.community", did, handle);
                yield return new BlueskyLinkTemplate("Red Dwarf", "reddwarf.ico", "reddwarf.app", did, handle);
            }
        }

        private async IAsyncEnumerable<ILinkTemplate> GetLinkTemplatesAsync()
        {
            yield return new FediverseLinkTemplate("ActivityPub");
            yield return new FediverseLinkTemplate("Mastodon", "mastodon.png");
            yield return new FediverseLinkTemplate("Pixelfed", "pixelfed.png");
            yield return new FediverseLinkTemplate("wafrn", "wafrn.png");
            yield return new BrowserPubLinkTemplate("browser.pub");

            await foreach (var x in GetBlueskyAppViewsAsync())
                yield return x;

            await foreach (var x in context.DeviantArtCredentials)
                yield return new DeviantArtLinkTemplate(x.Username);

            await foreach (var x in context.FurAffinityCredentials)
                yield return new FurAffinityLinkTemplate(x.Username);

            await foreach (var x in context.WeasylCredentials)
                yield return new WeasylLinkTemplate(x.Login);
        }

        private record Link(ILinkTemplate Template, PlatformLinkContext PlatformLinkContext) : IPlatformLink
        {
            public PlatformLinkCategory Category => Template.Category;
            public string? IconFilename => Template.IconFilename;
            public string? PlatformName => Template.PlatformName;
            public string? Username => PlatformLinkContext.IsProfile
                ? Template.Username
                : null;
            public string? Url => Template.GetUrl(PlatformLinkContext);
        }

        private IAsyncEnumerable<IPlatformLink> GetAllPlatformLinksAsync(PlatformLinkContext platformLinkContext) =>
            GetLinkTemplatesAsync()
            .Select(template => new Link(template, platformLinkContext))
            .Where(link => link.Username != null || link.Url != null);

        public async Task<IReadOnlyList<IPlatformLink>> GetProfileLinksAsync(
            CancellationToken cancellationToken = default)
        =>
            await GetAllPlatformLinksAsync(PlatformLinkContext.Profile).ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<IPlatformLink>> GetPostLinksAsync(
            IPlatformLinkPostSource post,
            CancellationToken cancellationToken = default)
        =>
            await GetAllPlatformLinksAsync(PlatformLinkContext.NewPost(post)).ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<string>> GetBlueskyStyleAppViewHostsAsync(
            CancellationToken cancellationToken)
        =>
            await GetBlueskyAppViewsAsync(cancellationToken)
            .Select(link => link.Host)
            .ToListAsync(cancellationToken);
    }
}
