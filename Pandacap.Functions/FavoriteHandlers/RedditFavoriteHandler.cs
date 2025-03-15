using Microsoft.EntityFrameworkCore;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Reddit;

namespace Pandacap.Functions.FavoriteHandlers
{
    public class RedditFavoriteHandler(
        PandacapDbContext context,
        RedditAppInformation redditAppInformation)
    {
        public async Task ImportUpvotesAsync()
        {
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

            Stack<Reddit.Controllers.Post> items = [];

            IEnumerable<Reddit.Controllers.Post> enumeratePostsAsync()
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

            foreach (var post in enumeratePostsAsync())
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
                var now = DateTimeOffset.UtcNow;
                var publishedTime = post.Created;
                var age = now - publishedTime;

                var thumbnail = post is Reddit.Controllers.LinkPost lp && lp.URL.StartsWith("https://i.redd.it/")
                    ? lp.Thumbnail
                    : null;

                context.RedditUpvotedPosts.Add(new()
                {
                    Id = Guid.NewGuid(),
                    Author = post.Author,
                    Id36 = post.Id,
                    Thumbnail = thumbnail,
                    Title = post.Title,
                    URL = "https://www.reddit.com" + post.Permalink,
                    FavoritedAt = age > TimeSpan.FromDays(28)
                        ? publishedTime
                        : now
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
