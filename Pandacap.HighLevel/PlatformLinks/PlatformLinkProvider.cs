using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Pandacap.ActivityPub;
using Pandacap.Clients.ATProto;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.Html;
using System.Runtime.CompilerServices;

namespace Pandacap.HighLevel.PlatformLinks
{
    public class PlatformLinkProvider(
        ActivityPubHostInformation activityPubHostInformation,
        ApplicationInformation appInfo,
        PandacapDbContext context,
        DIDResolver didResolver,
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache)
    {
        private const string KEY = "9d3b19b8-b641-4ea2-8f03-0edd775618d3";

        public async Task<IReadOnlyList<IPlatformLink>> GetPlatformLinksAsync(
            CancellationToken cancellationToken)
        =>
            await memoryCache.GetOrCreateAsync<IReadOnlyList<IPlatformLink>>(
                KEY,
                async _ =>
                {
                    var links = await GetUnderlyingPlatformLinksAsync(cancellationToken)
                        .ToListAsync(cancellationToken);

                    return await Task.WhenAll(
                        links
                        .Select(link => ResolveIconAsync(link, cancellationToken)));
                },
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(1)
                })
            ?? [];

        public async Task<IEnumerable<ActivityPubProfileLink>> GetActivityPubProfileLinksAsync(
            CancellationToken cancellationToken)
        =>
            await GetUnderlyingPlatformLinksAsync(cancellationToken)
                .Where(link => link.Category == PlatformLinkCategory.External)
                .Select(link => new ActivityPubProfileLink(
                    platformName: link.Host,
                    username: link.Username,
                    viewProfileUrl: link.ViewProfileUrl))
                .ToListAsync(cancellationToken);

        private async Task<IPlatformLink> ResolveIconAsync(
            IPlatformLink platformLink,
            CancellationToken cancellationToken)
        {
            if (platformLink.IconUrl != null)
                return platformLink;

            try
            {
                using var client = httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                using var req = new HttpRequestMessage(HttpMethod.Get, $"https://{platformLink.Host}");
                req.Headers.Accept.ParseAdd("text/html");
                using var resp = await client.SendAsync(req, cancellationToken);
                var html = await resp
                    .EnsureSuccessStatusCode()
                    .Content
                    .ReadAsStringAsync(cancellationToken);
                var href = ImageFinder
                    .FindFaviconsInHTML(html)
                    .LastOrDefault();
                if (href != null)
                {
                    return new ResolvedIconPlatformLink(
                        platformLink,
                        new Uri(req.RequestUri!, href).AbsoluteUri);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }

            return platformLink;
        }

        private async IAsyncEnumerable<IPlatformLink> GetUnderlyingPlatformLinksAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new BrowserPubLink(activityPubHostInformation, appInfo, "browser.pub");
            yield return new MastodonLink(appInfo, "mastodon.social");
            yield return new MastodonLink(appInfo, "activitypub.academy");
            yield return new MastodonLink(appInfo, "pixelfed.social");
            yield return new WafrnLink(appInfo, "app.wafrn.net");

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
                    var doc = await didResolver.ResolveAsync(did);
                    handle = doc?.Handle;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }

                yield return new BlueskyStyleATProtoPlatformLink("bsky.app", did, handle);
                yield return new BlueskyStyleATProtoPlatformLink("blacksky.community", did, handle);
                yield return new BlueskyStyleATProtoPlatformLink("reddwarf.app", did, handle);
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
