using Pandacap.Clients.ATProto;
using System.Runtime.CompilerServices;

namespace Pandacap.HighLevel
{
    public static class ConstellationClientExtensions
    {
        public static async IAsyncEnumerable<ATProtoRefUri> ListLinksAsync(
            this ConstellationClient constellationClient,
            string target,
            string collection,
            string path,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            string? cursor = null;

            while (true)
            {
                var page = await constellationClient.PageLinksAsync(
                    target,
                    collection,
                    path,
                    cursor,
                    cancellationToken);

                foreach (var item in page.linking_records)
                    yield return new($"at://{item.did}/{item.collection}/{item.rkey}");

                if (page.cursor is string next)
                    cursor = next;
                else
                    yield break;
            }
        }

        public static IAsyncEnumerable<ATProtoRefUri> ListLikesAsync(
            this ConstellationClient constellationClient,
            string did,
            string rkey,
            CancellationToken cancellationToken)
        {
            return ListLinksAsync(
                constellationClient,
                $"at://{did}/app.bsky.feed.post/{rkey}",
                "app.bsky.feed.like",
                ".subject.uri",
                cancellationToken);
        }

        public static IAsyncEnumerable<ATProtoRefUri> ListRepliesAsync(
            this ConstellationClient constellationClient,
            string did,
            string rkey,
            CancellationToken cancellationToken)
        {
            return ListLinksAsync(
                constellationClient,
                $"at://{did}/app.bsky.feed.post/{rkey}",
                "app.bsky.feed.post",
                ".reply.parent.uri",
                cancellationToken);
        }

        public static IAsyncEnumerable<ATProtoRefUri> ListRepostsAsync(
            this ConstellationClient constellationClient,
            string did,
            string rkey,
            CancellationToken cancellationToken)
        {
            return ListLinksAsync(
                constellationClient,
                $"at://{did}/app.bsky.feed.post/{rkey}",
                "app.bsky.feed.repost",
                ".subject.uri",
                cancellationToken);
        }

        public static IAsyncEnumerable<ATProtoRefUri> ListFollowsAsync(
            this ConstellationClient constellationClient,
            string did,
            CancellationToken cancellationToken)
        {
            return ListLinksAsync(
                constellationClient,
                $"{did}",
                "app.bsky.graph.follow",
                ".subject",
                cancellationToken);
        }

        public static IAsyncEnumerable<ATProtoRefUri> ListMentionsAsync(
            this ConstellationClient constellationClient,
            string did,
            CancellationToken cancellationToken)
        {
            return ListLinksAsync(
                constellationClient,
                $"{did}",
                "app.bsky.feed.post",
                ".facets[app.bsky.richtext.facet].features[app.bsky.richtext.facet#mention].did",
                cancellationToken);
        }
    }
}
