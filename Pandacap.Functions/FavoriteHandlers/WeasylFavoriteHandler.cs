using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.Weasyl.Interfaces;
using Pandacap.Weasyl.Models;
using Pandacap.Weasyl.Models.WeasylApi;

namespace Pandacap.Functions.FavoriteHandlers
{
    public partial class WeasylFavoriteHandler(
        PandacapDbContext context,
        UserAwareClientFactory userAwareClientFactory)
    {
        public async Task ImportFavoriteSubmissionsAsync(CancellationToken cancellationToken = default)
        {
            if (await userAwareClientFactory.CreateWeasylClientAsync(cancellationToken) is not IWeasylClient client)
                return;

            var self = await client.WhoamiAsync(cancellationToken);

            var tooNew = DateTimeOffset.UtcNow.AddMinutes(-5);

            Stack<Submission> items = [];

            await foreach (int submitid in client.ExtractFavoriteSubmitidsAsync(self.userid, cancellationToken))
            {
                var submission = await client.ViewSubmissionAsync(submitid, cancellationToken);

                var existing = await context.WeasylFavoriteSubmissions
                    .Where(item => item.Submitid == submission.submitid)
                    .ToListAsync(cancellationToken);

                if (existing.Count > 1)
                    context.RemoveRange(existing);
                else if (existing.Count > 0)
                    break;

                if (submission.rating != "general")
                    continue;

                items.Push(submission);

                if (items.Count >= 200)
                    break;
            }

            while (items.TryPop(out var submission))
            {
                context.WeasylFavoriteSubmissions.Add(new()
                {
                    Id = Guid.NewGuid(),
                    Submitid = submission.submitid,
                    Title = submission.title,
                    PostedBy = new()
                    {
                        Login = submission.owner_login,
                        DisplayName = submission.owner,
                        Avatar = submission.Avatars
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
                    FavoritedAt = DateTime.UtcNow.Date
                });
            }

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
