using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Pandacap.Clients.ATProto;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.HighLevel.ATProto;
using System.Drawing;
using System.Drawing.Imaging;

namespace Pandacap.HighLevel
{
    public class StarpassAgent(
        ATProtoCredentialProvider atProtoCredentialProvider,
        CompositeFavoritesProvider compositeFavoritesProvider,
        PandacapDbContext context,
        IHttpClientFactory httpClientFactory)
    {
        private static byte[] LetterboxToJpeg(byte[] data)
        {
            if (!OperatingSystem.IsWindows())
                throw new NotImplementedException();

            using var ms1 = new MemoryStream(data, writable: false);
            using var image1 = Image.FromStream(ms1);

            double w = image1.Width;
            double h = image1.Height;
            double r = w / h;

            if (w > 400)
            {
                double f = w / 400;
                w /= f;
                h /= f;
            }

            if (w > 200)
            {
                double f = h / 200;
                w /= f;
                h /= f;
            }

            using var image2 = new Bitmap(400, 200);
            using var g = Graphics.FromImage(image2);

            int iw = (int)w;
            int ih = (int)h;
            int ix = (image2.Width - iw) / 2;
            int iy = (image2.Height - ih) / 2;

            g.FillRectangle(new SolidBrush(Color.White), ix, iy, iw, ih);
            g.DrawImage(image1, ix, iy, iw, ih);

            using var ms2 = new MemoryStream();
            image2.Save(
                ms2,
                ImageFormat.Jpeg);

            return ms2.ToArray();
        }

        public async Task AddAsync(IFavorite favorite)
        {
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

            var letterboxedThumbnail = LetterboxToJpeg(thumbnailData);

            var postImage = await Repo.UploadBlobAsync(
                httpClient,
                credentials,
                letterboxedThumbnail,
                "image/jpeg",
                thumbnail.AltText);

            var record = await Repo.CreateRecordAsync(
                httpClient,
                credentials,
                Repo.Record.NewPost(new(
                    text: "",
                    createdAt: DateTimeOffset.UtcNow,
                    embed: Repo.PostEmbed.NewExternal(new(
                        description: $"by {favorite.Username}",
                        blob: postImage.Blob,
                        title: favorite.DisplayTitle,
                        uri: favorite.LinkUrl)),
                    pandacapMetadata: [
                        Repo.PandacapMetadata.NewFavoriteId(favorite.Id)
                    ])));

            starpassPost.BlueskyDID = credentials.DID;
            starpassPost.BlueskyRecordKey = record.RecordKey;
            await context.SaveChangesAsync();
        }

        public async Task RemoveAsync(StarpassPost starpassPost)
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

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

        public async Task RefreshAllAsync(int newPostLimit = int.MaxValue)
        {
            var cutoff = DateTime.UtcNow.AddDays(-14);

            var local = await compositeFavoritesProvider
                .GetAllAsync()
                .Where(p => p.Thumbnails.Any())
                .TakeWhile(p => p.FavoritedAt > cutoff)
                .OrderByDescending(post => post.FavoritedAt.Date)
                .ThenByDescending(post => post.PostedAt)
                .ToListAsync();

            var remote = await context.StarpassPosts
                .ToListAsync();

            var localIds = local
                .Select(x => x.Id)
                .ToHashSet();
            var remoteIds = remote
                .Select(x => x.FavoriteId)
                .ToHashSet();

            var toAdd = local
                .Where(x => !remoteIds.Contains(x.Id))
                .OrderBy(x => x.FavoritedAt)
                .Take(newPostLimit)
                .ToList();
            var toRemove = remote
                .Where(x => !localIds.Contains(x.FavoriteId))
                .ToList();

            var exceptions = new List<Exception>();

            foreach (var item in toAdd)
            {
                try
                {
                    await AddAsync(item);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            foreach (var item in toRemove)
            {
                try
                {
                    await RemoveAsync(item);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException([.. exceptions]);
            }
        }
    }
}
