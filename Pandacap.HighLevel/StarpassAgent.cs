using Microsoft.EntityFrameworkCore;
using Pandacap.Clients.ATProto;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.HighLevel.ATProto;

namespace Pandacap.HighLevel
{
    public class StarpassAgent(
        ATProtoCredentialProvider atProtoCredentialProvider,
        PandacapDbContext context,
        IHttpClientFactory httpClientFactory)
    {
        public async Task AddAsync(IFavorite favorite)
        {
            if (favorite is BlueskyFavorite || favorite is ActivityPubFavorite)
                throw new NotImplementedException();

            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var credentials = await atProtoCredentialProvider.GetStarpassCredentialsAsync();
            if (credentials == null)
                return;

            var starpassPost = await context.StarpassPosts
                .Where(s => s.FavoriteId == favorite.Id)
                .SingleOrDefaultAsync();

            if (starpassPost == null)
            {
                starpassPost = new StarpassPost
                {
                    FavoriteId = favorite.Id
                };

                context.StarpassPosts.Add(starpassPost);
            }

            var thumbnail = favorite.Thumbnails
                .Where(t => t.Url != null)
                .First();

            using var thumbnailResponse = await httpClient.GetAsync(thumbnail.Url);

            var thumbnailData = await thumbnailResponse
                .EnsureSuccessStatusCode()
                .Content
                .ReadAsByteArrayAsync();

            var postImage = await Repo.UploadBlobAsync(
                httpClient,
                credentials,
                thumbnailData,
                thumbnailResponse.Content.Headers.ContentType?.MediaType ?? "application/octet-stream",
                thumbnail.AltText);

            string platformName = favorite.Badges
                .Select(b => b.Text)
                .DefaultIfEmpty("an external site")
                .First();

            var record = await Repo.CreateRecordAsync(
                httpClient,
                credentials,
                new Repo.Post(
                    "",
                    favorite.PostedAt,
                    Repo.PostEmbed.NewExternal(new(
                        description: $"by {favorite.Username} on {platformName}",
                        blob: postImage.blob,
                        title: favorite.DisplayTitle,
                        uri: favorite.LinkUrl))));

            starpassPost.BlueskyDID = credentials.DID;
            starpassPost.BlueskyRecordKey = record.RecordKey;
            await context.SaveChangesAsync();
        }

        public async Task RemoveAsync(IFavorite favorite)
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var starpassPost = await context.StarpassPosts
                .Where(s => s.FavoriteId == favorite.Id)
                .SingleOrDefaultAsync();

            if (starpassPost == null)
                return;

            if (starpassPost.BlueskyDID != null)
            {
                var credentials = await atProtoCredentialProvider.GetCredentialsAsync(did: starpassPost.BlueskyDID);
                if (credentials != null)
                {
                    await Repo.DeleteRecordAsync(
                        httpClient,
                        credentials,
                        starpassPost.BlueskyRecordKey);
                }

                starpassPost.BlueskyDID = null;
                starpassPost.BlueskyRecordKey = null;
                await context.SaveChangesAsync();
            }

            context.StarpassPosts.Remove(starpassPost);
            await context.SaveChangesAsync();
        }
    }
}
