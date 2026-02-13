using Microsoft.EntityFrameworkCore;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Reddit;

namespace Pandacap.Functions.FavoriteHandlers
{
    public class RedditFavoriteHandler(
        PandacapDbContext context,
        IEnumerable<RedditAppInformation> anyRedditAppInformation)
    {
        public async Task ImportUpvotesAsync()
        {
            var redditAppInformation = anyRedditAppInformation.FirstOrDefault();
            if (redditAppInformation == null)
                return;

            var credentials = await context.RedditCredentials.SingleOrDefaultAsync();
            if (credentials == null)
                return;

            var client = new RedditClient(
                appId: redditAppInformation.AppId,
                appSecret: redditAppInformation.AppSecret,
                accessToken: credentials.AccessToken,
                refreshToken: credentials.RefreshToken,
                userAgent: UserAgentInformation.UserAgent);

            client.Models.OAuthCredentials.TokenUpdated += (o, e) =>
            {
                credentials.AccessToken = e.AccessToken;
                context.SaveChanges();
            };

            IEnumerable<Reddit.Controllers.Post> enumeratePosts()
            {
                string? after = null;

                while (true)
                {
                    var posts = client.User(credentials.Username).GetPostHistory(
                        where: "upvoted",
                        after: after);

                    if (posts.Count == 0)
                        yield break;

                    foreach (var post in posts)
                        yield return post;

                    after = posts.Last().Fullname;
                }
            }

            Stack<Reddit.Controllers.Post> items = [];

            foreach (var post in enumeratePosts())
            {
                var existing = await context.RedditUpvotedPosts
                    .Where(item => item.Id36 == post.Id)
                    .ToListAsync();

                if (existing.Count > 1)
                    context.RemoveRange(existing);
                else if (existing.Count > 0)
                    break;

                if (post.NSFW)
                    continue;

                items.Push(post);

                if (items.Count >= 50)
                    break;
            }

            while (items.TryPop(out var post))
            {
                var thumbnail = post is not Reddit.Controllers.LinkPost lp ? null
                    : lp.URL.StartsWith("https://i.redd.it/") ? lp.Thumbnail
                    : lp.URL.StartsWith("https://www.reddit.com/gallery") ? lp.Thumbnail
                    : null;

                context.RedditUpvotedPosts.Add(new()
                {
                    Id = Guid.NewGuid(),
                    Author = post.Author,
                    Created = post.Created,
                    Id36 = post.Id,
                    Thumbnail = thumbnail,
                    Title = post.Title,
                    URL = "https://www.reddit.com" + post.Permalink,
                    FavoritedAt = DateTime.UtcNow.Date
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
