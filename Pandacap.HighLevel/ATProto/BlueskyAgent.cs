using Azure.Storage.Blobs;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.Clients.ATProto;

namespace Pandacap.HighLevel.ATProto
{
    public class BlueskyAgent(
        ApplicationInformation appInfo,
        ATProtoCredentialProvider atProtoCredentialProvider,
        BlobServiceClient blobServiceClient,
        IHttpClientFactory httpClientFactory)
    {
        public async Task DeleteBlueskyPostsAsync(Post submission)
        {
            if (submission.BlueskyRecordKey == null)
                return;

            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var wrapper = await atProtoCredentialProvider.GetCredentialsAsync();

            if (wrapper?.DID != submission.BlueskyDID)
                throw new Exception("Cannot delete post from a non-connected atproto account");

            await Repo.DeleteRecordAsync(
                httpClient,
                wrapper,
                submission.BlueskyRecordKey);

            submission.BlueskyDID = null;
            submission.BlueskyRecordKey = null;
        }

        public async Task CreateBlueskyPostsAsync(Post submission)
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var wrapper = await atProtoCredentialProvider.GetCredentialsAsync();
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
                        .GetBlobClient($"{image.Blob.Id}")
                        .DownloadContentAsync();

                    yield return await Repo.UploadBlobAsync(
                        httpClient,
                        wrapper,
                        blob.Value.Content.ToArray(),
                        image.Blob.ContentType,
                        image.AltText);
                }
            }

            string text = submission.Body;
            int codepoints = text.Where(c => !char.IsLowSurrogate(c)).Count();
            if (codepoints >= 300)
            {
                text = $"{submission.Title}\n\nhttps://{appInfo.ApplicationHostname}/UserPosts/{submission.Id}";
            }

            var post = await Repo.CreateRecordAsync(
                httpClient,
                wrapper,
                new Repo.Post(
                    text: text,
                    createdAt: submission.PublishedTime,
                    embed: Repo.PostEmbed.NewImages([.. await downloadImagesAsync().ToListAsync()]),
                    pandacapIds: [Repo.PandacapId.NewForPost(submission.Id)]));

            submission.BlueskyDID = wrapper.DID;
            submission.BlueskyRecordKey = post.RecordKey;
        }
    }
}
