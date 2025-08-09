using Microsoft.FSharp.Collections;
using Pandacap.Data;
using Pandacap.PlatformBadges;

namespace Pandacap
{
    public static class BadgeBuilder
    {
        public static FSharpList<Badge> GetBadges(PostPlatform platform, string url)
        {
            if (platform == PostPlatform.Pandacap)
                return [];

            var defaultBadge = PostPlatformModule.GetBadge(platform);

            if (url == null)
                return [defaultBadge];

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
                return [defaultBadge];

            if (uri.Host == "bsky.brid.gy")
                return [new(
                    uri.Host,
                    "white",
                    "#3c8fff")];

            return [new(
                uri.Host,
                defaultBadge.Background,
                defaultBadge.Color)];
        }

        public static FSharpList<Badge> GetBadges(this IPost post) =>
            GetBadges(post.Platform, post.Url);

        public static FSharpList<Badge> GetBadges(this IFollow follow) =>
            GetBadges(follow.Platform, follow.Url);
    }
}
