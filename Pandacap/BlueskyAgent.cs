using Azure.Storage.Blobs;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.Clients.ATProto.Private;
using Pandacap.HighLevel.ATProto;
using Microsoft.EntityFrameworkCore;

namespace Pandacap
{
    public class BlueskyAgent(
        ATProtoCredentialProvider atProtoCredentialProvider,
        BlobServiceClient blobServiceClient,
        PandacapDbContext context,
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

        public async Task LikeBlueskyPostAsync(Guid id, string did)
        {
            IBlueskyPost? dbPost =
                await context.BlueskyFavorites.SingleOrDefaultAsync(b => b.Id == id)
                ?? await context.BlueskyFeedItems.SingleOrDefaultAsync(b => b.Id == id)
                ?? (IBlueskyPost?)null
                ?? throw new Exception("Post not found in Favorites or Inbox");

            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var wrapper = await atProtoCredentialProvider.GetCredentialsAsync(did);
            if (wrapper == null)
                return;

            var like = await Repo.CreateRecordAsync(
                httpClient,
                wrapper,
                Repo.Record.NewLike(new(
                    uri: $"at://{dbPost.DID}/app.bsky.feed.post/{dbPost.RecordKey}",
                    cid: dbPost.CID)));

            context.BlueskyLikes.Add(new()
            {
                Id = Guid.NewGuid(),
                DID = did,
                SubjectCID = dbPost.CID,
                SubjectRecordKey = dbPost.RecordKey,
                LikeCID = like.cid,
                LikeRecordKey = like.RecordKey
            });

            await context.SaveChangesAsync();
        }

        public async Task UnlikeBlueskyPostAsync(Guid id, string did)
        {
            IBlueskyPost? dbPost =
                await context.BlueskyFavorites.SingleOrDefaultAsync(b => b.Id == id)
                ?? await context.BlueskyFeedItems.SingleOrDefaultAsync(b => b.Id == id)
                ?? (IBlueskyPost?)null
                ?? throw new Exception("Post not found in Favorites or Inbox");

            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            await foreach (var like in context.BlueskyLikes
                .Where(l => l.SubjectCID == dbPost.CID)
                .AsAsyncEnumerable())
            {
                context.Remove(like);
            }

            await context.SaveChangesAsync();
        }
    }
}
