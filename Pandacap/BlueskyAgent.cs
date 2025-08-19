using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Pandacap.Clients.ATProto.Private;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.HighLevel.ATProto;

namespace Pandacap
{
    public class BlueskyAgent(
        ATProtoCredentialProvider atProtoCredentialProvider,
        BlobServiceClient blobServiceClient,
        PandacapDbContext context,
        IHttpClientFactory httpClientFactory)
    {
        private async Task<Clients.ATProto.Public.BlueskyFeed.Post> FetchBlueskyPostAsync(string pds, string did, string rkey)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var posts = await Clients.ATProto.Public.BlueskyFeed.GetPostsAsync(
                client,
                pds,
                [$"at://{did}/app.bsky.feed.post/{rkey}"]);

            return posts.posts.Single();
        }

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

        public async Task LikeBlueskyPostAsync(string pds, string author_did, string rkey, string my_did)
        {
            var post = await FetchBlueskyPostAsync(pds, author_did, rkey);

            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var wrapper = await atProtoCredentialProvider.GetCredentialsAsync(my_did);
            if (wrapper == null)
                return;

            var like = await Repo.CreateRecordAsync(
                httpClient,
                wrapper,
                Repo.Record.NewLike(new(
                    uri: post.uri,
                    cid: post.cid)));

            context.BlueskyLikes.Add(new()
            {
                Id = Guid.NewGuid(),
                DID = my_did,
                SubjectCID = post.cid,
                SubjectRecordKey = post.RecordKey,
                LikeCID = like.cid,
                LikeRecordKey = like.RecordKey
            });

            await context.SaveChangesAsync();
        }

        public async Task UnlikeBlueskyPostAsync(string rkey, string my_did)
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var credentials = await atProtoCredentialProvider.GetCredentialsAsync(my_did);

            await foreach (var like in context.BlueskyLikes
                .Where(l => l.DID == my_did)
                .Where(l => l.SubjectRecordKey == rkey)
                .AsAsyncEnumerable())
            {
                if (credentials != null)
                    await Repo.DeleteRecordAsync(
                        httpClient,
                        credentials,
                        "app.bsky.feed.like",
                        rkey);

                context.Remove(like);
                await context.SaveChangesAsync();
            }
        }
    }
}
