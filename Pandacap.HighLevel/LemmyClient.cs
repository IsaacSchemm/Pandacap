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
            var resp = await Lemmy.GetCommunityAsync(client, host, name, cancellationToken);
            return resp.community_view.community;
        }

        public async Task<FSharpList<Lemmy.PostObject>> GetPostsAsync(
            string host,
            int community_id,
            Lemmy.Sort sort,
            int page = 1,
            int limit = 10,
            CancellationToken cancellationToken = default)
        {
            using var client = httpClientFactory.CreateClient();

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

        [Obsolete("Unused")]
        public async IAsyncEnumerable<Lemmy.PostObject> GetPostsAsync(
            string host,
            int community_id,
            Lemmy.Sort sort,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var client = httpClientFactory.CreateClient();

            IEnumerable<Lemmy.GetPostsParameter> defaultParameters = [
                Lemmy.GetPostsParameter.NewSort(sort),
                Lemmy.GetPostsParameter.NewCommunityId(community_id)
            ];

            var parameters = defaultParameters;

            while (true)
            {
                var page = await Lemmy.GetPostsAsync(client, host, parameters, cancellationToken);

                foreach (var post in page.posts)
                    yield return post;

                if (!page.HasNextPage)
                    break;

                parameters = [
                    ..defaultParameters,
                    Lemmy.GetPostsParameter.NewPageCursor(page.next_page.Value)
                ];
            }
        }
    }
}
