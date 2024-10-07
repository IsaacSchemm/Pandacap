using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Collections;
using Pandacap.Data;
using Pandacap.LowLevel;

namespace Pandacap.HighLevel
{
    public class ExternalPlatformProvider(
        ApplicationInformation appInfo,
        PandacapDbContext context)
    {
        public async IAsyncEnumerable<ExternalPlatform> GetExternalPlatformsAsync()
        {
            yield return new ExternalPlatform(
                "ActivityPub",
                $"@{appInfo.Username}@{appInfo.ApplicationHostname}",
                $"https://{appInfo.ApplicationHostname}",
                ListModule.OfArray([
                    new ExternalPlatformAdditionalLink(
                        "Followers",
                        $"https://{appInfo.ApplicationHostname}/Profile/Followers"),
                    new ExternalPlatformAdditionalLink(
                        "Following",
                        $"https://{appInfo.ApplicationHostname}/Profile/Following"),
                    new ExternalPlatformAdditionalLink(
                        "Favorites",
                        $"https://{appInfo.ApplicationHostname}/Favorites")
                ]));

            await foreach (string did in context.ATProtoCredentials.Select(c => c.DID).AsAsyncEnumerable())
                yield return new ExternalPlatform(
                    "Bluesky",
                    did,
                    $"https://bsky.app/profile/{did}",
                    ListModule.OfArray([
                        new ExternalPlatformAdditionalLink(
                            "Followers",
                            $"https://bsky.app/profile/{did}/followers"),
                        new ExternalPlatformAdditionalLink(
                            "Following",
                            $"https://bsky.app/profile/{did}/follows")
                    ]));

            yield return new ExternalPlatform(
                "DeviantArt",
                appInfo.DeviantArtUsername,
                $"https://www.deviantart.com/{Uri.EscapeDataString(appInfo.DeviantArtUsername)}",
                ListModule.OfArray([
                    new ExternalPlatformAdditionalLink(
                        "Watchers",
                        $"https://www.deviantart.com/{Uri.EscapeDataString(appInfo.DeviantArtUsername)}/about#watchers"),
                    new ExternalPlatformAdditionalLink(
                        "Watching",
                        $"https://www.deviantart.com/{Uri.EscapeDataString(appInfo.DeviantArtUsername)}/about#watching"),
                    new ExternalPlatformAdditionalLink(
                        "Favorites",
                        $"https://www.deviantart.com/{Uri.EscapeDataString(appInfo.DeviantArtUsername)}/favourites/all")
                ]));

            await foreach (string username in context.WeasylCredentials.Select(c => c.Login).AsAsyncEnumerable())
                yield return new ExternalPlatform(
                    "Weasyl",
                    username,
                    $"https://www.weasyl.com/profile/~{Uri.EscapeDataString(username)}",
                    ListModule.OfArray([
                        new ExternalPlatformAdditionalLink(
                            "Followers",
                            $"https://www.weasyl.com/followed/{Uri.EscapeDataString(username)}"),
                        new ExternalPlatformAdditionalLink(
                            "Following",
                            $"https://www.weasyl.com/following/{Uri.EscapeDataString(username)}"),
                        new ExternalPlatformAdditionalLink(
                            "Favorites",
                            $"https://www.weasyl.com/favorites/{Uri.EscapeDataString(username)}")
                    ]));
        }
    }
}
