using Pandacap.ATProto.Models;
using Pandacap.ATProto.Services.Interfaces;

namespace Pandacap.HighLevel
{
    public static class ConstellationClientExtensions
    {
        public static IAsyncEnumerable<ATProtoRefUri> ListLikesAsync(
            this IConstellationService constellationService,
            string did,
            string rkey)
        {
            return constellationService.GetLinksAsync(
                $"at://{did}/app.bsky.feed.post/{rkey}",
                "app.bsky.feed.like",
                ".subject.uri");
        }

        public static IAsyncEnumerable<ATProtoRefUri> ListRepliesAsync(
            this IConstellationService constellationService,
            string did,
            string rkey)
        {
            return constellationService.GetLinksAsync(
                $"at://{did}/app.bsky.feed.post/{rkey}",
                "app.bsky.feed.post",
                ".reply.parent.uri");
        }

        public static IAsyncEnumerable<ATProtoRefUri> ListRepostsAsync(
            this IConstellationService constellationService,
            string did,
            string rkey)
        {
            return constellationService.GetLinksAsync(
                $"at://{did}/app.bsky.feed.post/{rkey}",
                "app.bsky.feed.repost",
                ".subject.uri");
        }

        public static IAsyncEnumerable<ATProtoRefUri> ListFollowsAsync(
            this IConstellationService constellationService,
            string did)
        {
            return constellationService.GetLinksAsync(
                $"{did}",
                "app.bsky.graph.follow",
                ".subject");
        }

        public static IAsyncEnumerable<ATProtoRefUri> ListMentionsAsync(
            this IConstellationService constellationService,
            string did)
        {
            return constellationService.GetLinksAsync(
                $"{did}",
                "app.bsky.feed.post",
                ".facets[app.bsky.richtext.facet].features[app.bsky.richtext.facet#mention].did");
        }
    }
}
