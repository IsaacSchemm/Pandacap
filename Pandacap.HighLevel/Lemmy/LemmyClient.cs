using Microsoft.FSharp.Collections;
using Pandacap.ConfigurationObjects;
using System.Runtime.CompilerServices;

namespace Pandacap.HighLevel.Lemmy
{
    public class LemmyClient(
        IHttpClientFactory httpClientFactory)
    {
        public async Task<LowLevel.Lemmy.Community> GetCommunityAsync(
            string host,
            string name,
            CancellationToken cancellationToken = default)
        {
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);
            var resp = await LowLevel.Lemmy.GetCommunityAsync(client, host, name, cancellationToken);
            return resp.community_view.community;
        }

        public async Task<(LowLevel.Lemmy.PostView, LowLevel.Lemmy.Community)> GetPostAsync(
            string host,
            int id,
            CancellationToken cancellationToken = default)
        {
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);
            var resp = await LowLevel.Lemmy.GetPostAsync(client, host, id, cancellationToken);
            return (resp.post_view, resp.community_view.community);
        }

        public async Task<FSharpList<LowLevel.Lemmy.PostView>> GetPostsAsync(
            string host,
            int community_id,
            LowLevel.Lemmy.GetPostsSort sort,
            int page = 1,
            int limit = 10,
            CancellationToken cancellationToken = default)
        {
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var resp = await LowLevel.Lemmy.GetPostsAsync(
                client,
                host,
                [
                    LowLevel.Lemmy.GetPostsParameter.NewSort(sort),
                    LowLevel.Lemmy.GetPostsParameter.NewPage(page),
                    LowLevel.Lemmy.GetPostsParameter.NewLimit(limit),
                    LowLevel.Lemmy.GetPostsParameter.NewCommunityId(community_id)
                ],
                cancellationToken);

            return resp.posts;
        }

        public async IAsyncEnumerable<LowLevel.Lemmy.CommentObject> GetCommentsAsync(
            string host,
            int post_id,
            LowLevel.Lemmy.GetCommentsSort sort,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            int page = 1;

            while (true)
            {
                var resp = await LowLevel.Lemmy.GetCommentsAsync(
                    client,
                    host,
                    [
                        LowLevel.Lemmy.GetCommentsParameter.NewSort(sort),
                        LowLevel.Lemmy.GetCommentsParameter.NewPostId(post_id),
                        LowLevel.Lemmy.GetCommentsParameter.NewPage(page)
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
