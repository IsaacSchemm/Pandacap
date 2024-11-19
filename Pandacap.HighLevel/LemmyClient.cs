using Microsoft.FSharp.Collections;
using Pandacap.LowLevel;
using System.Runtime.CompilerServices;

namespace Pandacap.HighLevel
{
    public class LemmyClient(
        IHttpClientFactory httpClientFactory)
    {
        public async Task<Lemmy.Community> GetCommunityAsync(
            string host,
            string name,
            CancellationToken cancellationToken = default)
        {
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);
            var resp = await Lemmy.GetCommunityAsync(client, host, name, cancellationToken);
            return resp.community_view.community;
        }

        public async Task<(Lemmy.PostView, Lemmy.Community)> GetPostAsync(
            string host,
            int id,
            CancellationToken cancellationToken = default)
        {
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);
            var resp = await Lemmy.GetPostAsync(client, host, id, cancellationToken);
            return (resp.post_view, resp.community_view.community);
        }

        public async Task<FSharpList<Lemmy.PostView>> GetPostsAsync(
            string host,
            int community_id,
            Lemmy.GetPostsSort sort,
            int page = 1,
            int limit = 10,
            CancellationToken cancellationToken = default)
        {
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var resp = await Lemmy.GetPostsAsync(
                client,
                host,
                [
                    Lemmy.GetPostsParameter.NewSort(sort),
                    Lemmy.GetPostsParameter.NewPage(page),
                    Lemmy.GetPostsParameter.NewLimit(limit),
                    Lemmy.GetPostsParameter.NewCommunityId(community_id)
                ],
                cancellationToken);

            return resp.posts;
        }

        public async IAsyncEnumerable<Lemmy.CommentObject> GetCommentsAsync(
            string host,
            int post_id,
            Lemmy.GetCommentsSort sort,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            int page = 1;

            while (true)
            {
                var resp = await Lemmy.GetCommentsAsync(
                    client,
                    host,
                    [
                        Lemmy.GetCommentsParameter.NewSort(sort),
                        Lemmy.GetCommentsParameter.NewPostId(post_id),
                        Lemmy.GetCommentsParameter.NewPage(page)
                    ],
                    cancellationToken);

                if (resp.comments.Length == 0)
                    break;

                foreach (var comment in resp.comments)
                    yield return comment;

                page++;
            }
        }
    }
}
