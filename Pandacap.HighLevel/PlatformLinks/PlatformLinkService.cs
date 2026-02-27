using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Pandacap.ActivityPub;
using Pandacap.Clients.ATProto;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pandacap.HighLevel.PlatformLinks
{
    public class PlatformLinkService(
        ActivityPubHostInformation activityPubHostInformation,
        ApplicationInformation appInfo,
        PandacapDbContext context,
        DIDResolver didResolver)
    {
        public async IAsyncEnumerable<IPlatformLink> GetPlatformLinksAsync()
        {
            yield return new MastodonLink(appInfo, "mastodon.social");
            yield return new MastodonLink(appInfo, "activitypub.academy");
            yield return new PixelfedLink(appInfo, "pixelfed.social");
            yield return new WafrnLink(appInfo, "app.wafrn.net");
            yield return new BrowserPubLink(activityPubHostInformation, appInfo, "browser.pub");

            var did = await context.Posts
                .OrderByDescending(post => post.PublishedTime)
                .Where(post => post.BlueskyDID != null)
                .Select(post => post.BlueskyDID)
                .FirstOrDefaultAsync();

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

                yield return new BlueskyStyleATProtoPlatformLink(
                    "Bluesky",
                    "https://web-cdn.bsky.app/static/favicon-16x16.png",
                    "bsky.app",
                    did,
                    handle);

                yield return new BlueskyStyleATProtoPlatformLink(
                    "Blacksky",
                    "https://blacksky.community/static/favicon-16x16.png",
                    "blacksky.community",
                    did,
                    handle);

                yield return new BlueskyStyleATProtoPlatformLink(
                    "Red Dwarf",
                    "https://reddwarf.app/redstar.png",
                    "reddwarf.app",
                    did,
                    handle);
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
