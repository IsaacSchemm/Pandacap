using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using System.Threading.Tasks;

namespace Pandacap
{
    public class CompositeFavoritesProvider(PandacapDbContext context)
    {
        public async IAsyncEnumerable<IFavorite> GetAllAsync()
        {
            if (await context.ActivityPubLikes.CountAsync() == 0)
            {
                await foreach (var like in context.RemoteActivityPubFavorites)
                {
                    context.ActivityPubLikes.Add(new()
                    {
                        Attachments = [.. like.Attachments.Select(a => new ActivityPubFavoriteImage
                        {
                            Name = a.Name,
                            Url = a.Url
                        })],
                        Content = like.Content,
                        CreatedAt = like.CreatedAt,
                        CreatedBy = like.CreatedBy,
                        FavoritedAt = like.FavoritedAt,
                        InReplyTo = like.InReplyTo,
                        LikeGuid = like.LikeGuid,
                        Name = like.Name,
                        ObjectId = like.ObjectId,
                        Sensitive = like.Sensitive,
                        Summary = like.Summary,
                        Usericon = like.Usericon,
                        Username = like.Username
                    });
                }

                await context.SaveChangesAsync();
            }

            var activityPubAnnounces = context.ActivityPubAnnounces
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IFavorite>();

            var activityPubLikes = context.ActivityPubLikes
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IFavorite>();

            var blueskyLikes = context.BlueskyLikes
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IFavorite>();

            var blueskyReposts = context.BlueskyReposts
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IFavorite>();

            var deviantArtFavorites = context.DeviantArtFavorites
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IFavorite>();

            var furAffinityFavorites = context.FurAffinityFavorites
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IFavorite>();

            var weasylFavoriteSubmissions = context.WeasylFavoriteSubmissions
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IFavorite>();

            var x = new[]
            {
                activityPubAnnounces,
                activityPubLikes,
                blueskyLikes,
                blueskyReposts,
                deviantArtFavorites,
                furAffinityFavorites,
                weasylFavoriteSubmissions
            }
            .MergeNewest(post => post.Timestamp)
            .Where(post => post.HiddenAt == null);

            await foreach (var y in x) yield return y;
        }
    }
}
