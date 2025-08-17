using Azure.Storage.Blobs;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.Clients.ATProto.Private;
using Pandacap.HighLevel.ATProto;

namespace Pandacap
{
    public class BlueskyAgent(
        ATProtoCredentialProvider atProtoCredentialProvider,
        BlobServiceClient blobServiceClient,
        IHttpClientFactory httpClientFactory)
    {
        public async Task CreateBlueskyPostAsync(Post submission, string text)
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var wrapper = await atProtoCredentialProvider.GetCrosspostingCredentialsAsync();
            if (wrapper == null)
                return;

            if (wrapper.DID == submission.BlueskyDID)
                return;

            async IAsyncEnumerable<Repo.PostImage> downloadImagesAsync()
            {
                foreach (var image in submission.Images)
                {
                    var blob = await blobServiceClient
                        .GetBlobContainerClient("blobs")
                        .GetBlobClient($"{image.Raster.Id}")
                        .DownloadContentAsync();

                    yield return await Repo.UploadBlobAsync(
                        httpClient,
                        wrapper,
                        blob.Value.Content.ToArray(),
                        image.Raster.ContentType,
                        image.AltText);
                }
            }

            var post = await Repo.CreateRecordAsync(
                httpClient,
                wrapper,
                Repo.Record.NewPost(new(
                    text: text,
                    createdAt: submission.PublishedTime,
                    embed: Repo.PostEmbed.NewImages([
                        .. await downloadImagesAsync().ToListAsync()
                    ]),
                    pandacapMetadata: [
                        Repo.PandacapMetadata.NewPostId(submission.Id)
                    ])));

            submission.BlueskyDID = wrapper.DID;
            submission.BlueskyRecordKey = post.RecordKey;
        }

        public async Task LikeBlueskyPostAsync(BlueskyFavorite favorite, string did)
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var wrapper = await atProtoCredentialProvider.GetCredentialsAsync(did);
            if (wrapper == null)
                return;

            var like = await Repo.CreateRecordAsync(
                httpClient,
                wrapper,
                Repo.Record.NewLike(new(
                    uri: $"at://{favorite.CreatedBy.DID}/app.bsky.feed.post/{favorite.RecordKey}",
                    cid: favorite.CID)));

            favorite.Likes ??= [];
            favorite.Likes.Add(new()
            {
                DID = wrapper.DID,
                RecordKey = like.RecordKey
            });
        }

        public async Task UnlikeBlueskyPostAsync(BlueskyFavorite favorite, string did)
        {
            favorite.Likes ??= [];

            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            foreach (var like in favorite.Likes)
            {
                var wrapper = await atProtoCredentialProvider.GetCredentialsAsync(did);
                if (wrapper != null)
                    await Repo.DeleteRecordAsync(
                        httpClient,
                        wrapper,
                        "app.bsky.feed.like",
                        like.RecordKey);
            }

            favorite.Likes = [];
        }
    }
}
