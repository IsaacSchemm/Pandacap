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
            if (submission.BlueskyCrossposts.Count == 0)
                return;

            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(appInfo.UserAgent);

            foreach (var mirrorPost in submission.BlueskyCrossposts)
            {
                try
                {
                    var wrapper = await atProtoCredentialProvider.GetCredentialsAsync();

                    if (wrapper?.DID != mirrorPost.DID)
                        continue;

                    await Repo.DeleteRecordAsync(
                        httpClient,
                        wrapper,
                        mirrorPost.RecordKey);

                    submission.BlueskyCrossposts.Remove(mirrorPost);
                }
                catch (Exception) { }
            }
        }

        public async Task CreateBlueskyPostsAsync(UserPost submission)
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(appInfo.UserAgent);

            var wrapper = await atProtoCredentialProvider.GetCredentialsAsync();
            if (wrapper == null)
                return;

            if (!wrapper.Crosspost)
                return;

            if (submission.BlueskyCrossposts.Any(m => m.DID == wrapper.DID))
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

            var post = await Repo.CreateRecordAsync(
                httpClient,
                wrapper,
                new Repo.Post(
                    text: submission.DescriptionText,
                    createdAt: submission.PublishedTime,
                    images: await downloadImagesAsync().ToListAsync()));

            submission.BlueskyCrossposts.Add(new()
            {
                DID = wrapper.DID,
                RecordKey = post.RecordKey
            });
        }
    }
}
