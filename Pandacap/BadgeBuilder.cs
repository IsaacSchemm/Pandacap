using Microsoft.FSharp.Collections;
using Pandacap.Data;
using Pandacap.PlatformBadges;

namespace Pandacap
{
    public static class BadgeBuilder
    {
        public static FSharpList<Badge> GetBadges(PostPlatform platform, string url)
        {
            var defaultBadge = PostPlatformModule.GetBadge(platform);

            if (url != null && Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
                return [new(uri.Host, defaultBadge.Background, defaultBadge.Color)];

            if (platform == PostPlatform.Pandacap)
                return [];

            return [defaultBadge];
        }

        public static FSharpList<Badge> GetBadges(this IPost post) =>
            GetBadges(post.Platform, post.Url);
    }
}
