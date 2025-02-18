using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.HighLevel.Weasyl;

namespace Pandacap.Functions.FavoriteHandlers
{
    public partial class WeasylFavoriteHandler(
        PandacapDbContext context,
        WeasylClientFactory weasylClientFactory)
    {
        public async Task ImportFavoriteSubmissionsAsync()
        {
            if (await weasylClientFactory.CreateWeasylClientAsync() is not WeasylClient client)
                return;

            var self = await client.WhoamiAsync();

            var tooNew = DateTimeOffset.UtcNow.AddMinutes(-5);

            Stack<WeasylClient.Submission> items = [];

            await foreach (int submitid in client.ExtractFavoriteSubmitidsAsync(self.userid))
            {
                var submission = await client.ViewSubmissionAsync(submitid);

                var existing = await context.WeasylFavoriteSubmissions
                    .Where(item => item.Submitid == submission.submitid)
                    .CountAsync();
                if (existing > 0)
                    break;

                if (submission.rating != "general")
                    continue;

                items.Push(submission);

                if (items.Count >= 200)
                    break;
            }

            while (items.TryPop(out var submission))
            {
                var now = DateTimeOffset.UtcNow;
                var publishedTime = submission.posted_at;
                var age = now - publishedTime;

                context.WeasylFavoriteSubmissions.Add(new()
                {
                    Id = Guid.NewGuid(),
                    Submitid = submission.submitid,
                    Title = submission.title,
                    PostedBy = new()
                    {
                        Login = submission.owner_login,
                        DisplayName = submission.owner,
                        Avatar = (submission.owner_media?.avatar ?? [])
                            .Select(a => a.url)
                            .FirstOrDefault()
                    },
                    PostedAt = submission.posted_at,
                    Thumbnails = [
                        .. submission.media.thumbnail
                            .Select(t => new WeasylFavoriteSubmissionImage
                            {
                                Url = t.url
                            })
                    ],
                    Url = submission.link,
                    FavoritedAt = age > TimeSpan.FromDays(1)
                        ? publishedTime
                        : now
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
