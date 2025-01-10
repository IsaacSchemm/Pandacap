using Microsoft.FSharp.Collections;
using Pandacap.ConfigurationObjects;
using System.Runtime.CompilerServices;

namespace Pandacap.HighLevel.Lemmy
{
    public class LemmyClient(
        IHttpClientFactory httpClientFactory)
    {
        public async Task<Clients.Lemmy.Community> GetCommunityAsync(
            string host,
            string name,
            CancellationToken cancellationToken = default)
        {
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);
            var resp = await Clients.Lemmy.GetCommunityAsync(client, host, name, cancellationToken);
            return resp.community_view.community;
        }

        public async Task<(Clients.Lemmy.PostView, Clients.Lemmy.Community)> GetPostAsync(
            string host,
            int id,
            CancellationToken cancellationToken = default)
        {
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);
            var resp = await Clients.Lemmy.GetPostAsync(client, host, id, cancellationToken);
            return (resp.post_view, resp.community_view.community);
        }

        public async Task<FSharpList<Clients.Lemmy.PostView>> GetPostsAsync(
            string host,
            int community_id,
            Clients.Lemmy.GetPostsSort sort,
            int page = 1,
            int limit = 10,
            CancellationToken cancellationToken = default)
        {
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var resp = await Clients.Lemmy.GetPostsAsync(
                client,
                host,
                [
                    Clients.Lemmy.GetPostsParameter.NewSort(sort),
                    Clients.Lemmy.GetPostsParameter.NewPage(page),
                    Clients.Lemmy.GetPostsParameter.NewLimit(limit),
                    Clients.Lemmy.GetPostsParameter.NewCommunityId(community_id)
                ],
                cancellationToken);

            return resp.posts;
        }

        public async IAsyncEnumerable<Clients.Lemmy.CommentObject> GetCommentsAsync(
            string host,
            int post_id,
            Clients.Lemmy.GetCommentsSort sort,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            int page = 1;

            while (true)
            {
                var resp = await Clients.Lemmy.GetCommentsAsync(
                    client,
                    host,
                    [
                        Clients.Lemmy.GetCommentsParameter.NewSort(sort),
                        Clients.Lemmy.GetCommentsParameter.NewPostId(post_id),
                        Clients.Lemmy.GetCommentsParameter.NewPage(page)
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
