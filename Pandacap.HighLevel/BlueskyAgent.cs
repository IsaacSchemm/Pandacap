using Azure.Storage.Blobs;
using Pandacap.Data;
using Pandacap.LowLevel;
using Pandacap.LowLevel.ATProto;

namespace Pandacap.HighLevel
{
    public class BlueskyAgent(
        ApplicationInformation appInfo,
        ATProtoCredentialProvider atProtoCredentialProvider,
        BlobServiceClient blobServiceClient,
        IHttpClientFactory httpClientFactory)
    {
        public async Task DeleteBlueskyPostsAsync(UserPost submission)
        {
            if (submission.BlueskyRecordKey != null)
                return;

            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(appInfo.UserAgent);

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

        public async Task CreateBlueskyPostsAsync(UserPost submission)
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(appInfo.UserAgent);

            var wrapper = await atProtoCredentialProvider.GetCredentialsAsync();
            if (wrapper == null)
                return;

            if (wrapper.DID == submission.BlueskyDID)
                return;

            async IAsyncEnumerable<Repo.BlobWithAltText> downloadImagesAsync()
            {
                if (submission.Image == null)
                    yield break;

                var blob = await blobServiceClient
                    .GetBlobContainerClient("blobs")
                    .GetBlobClient(submission.Image.BlobName)
                    .DownloadContentAsync();

                yield return await Repo.UploadBlobAsync(
                    httpClient,
                    wrapper,
                    blob.Value.Content.ToArray(),
                    submission.Image.ContentType,
                    submission.AltText);
            }

            string text = submission.DescriptionText;
            int codepoints = text.Where(c => !char.IsLowSurrogate(c)).Count();
            if (codepoints >= 300)
            {
                text = submission.Title + "\n\n" + submission.Url;
            }

            var post = await Repo.CreateRecordAsync(
                httpClient,
                wrapper,
                new Repo.Post(
                    text: text,
                    createdAt: submission.PublishedTime,
                    images: await downloadImagesAsync().ToListAsync()));

            submission.BlueskyDID = wrapper.DID;
            submission.BlueskyRecordKey = post.RecordKey;
        }
    }
}
