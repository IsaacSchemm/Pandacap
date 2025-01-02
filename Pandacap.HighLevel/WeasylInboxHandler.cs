using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.LowLevel;

namespace Pandacap.HighLevel
{
    public class WeasylInboxHandler(
        PandacapDbContext context,
        WeasylClientFactory weasylClientFactory)
    {
        /// <summary>
        /// Imports new posts from the past three days that have not yet been
        /// added to the Pandacap inbox.
        /// </summary>
        /// <returns></returns>
        public async Task ImportSubmissionsByUsersWeWatchAsync()
        {
            if (await weasylClientFactory.CreateWeasylClientAsync() is not WeasylClient weasylClient)
                return;

            DateTimeOffset someTimeAgo = DateTimeOffset.UtcNow.AddDays(-3);

            var existingPosts = await context.InboxWeasylSubmissions
                .Where(item => item.PostedAt >= someTimeAgo)
                .ToListAsync();

            Dictionary<string, WeasylClient.AvatarResponse> avatars = [];

            await foreach (var submission in weasylClient.GetMessagesSubmissionsAsync())
            {
                if (submission.posted_at < someTimeAgo)
                    break;

                if (existingPosts.Any(e => e.Submitid == submission.submitid))
                    continue;

                if (!avatars.TryGetValue(submission.owner_login, out var avatarResponse))
                {
                    avatarResponse = await weasylClient.GetAvatarAsync(submission.owner_login);
                    avatars[submission.owner_login] = avatarResponse;
                }

                context.InboxWeasylSubmissions.Add(new()
                {
                    Id = Guid.NewGuid(),
                    Submitid = submission.submitid,
                    Title = submission.title,
                    Rating = submission.rating,
                    PostedBy = new InboxWeasylUser
                    {
                        Login = submission.owner_login,
                        DisplayName = submission.owner,
                        Avatar = avatarResponse.avatar
                    },
                    PostedAt = submission.posted_at,
                    Thumbnails = submission.media.thumbnail
                        .Select(t => new InboxWeasylImage {
                            Url = t.url
                        })
                        .ToList(),
                    Url = submission.link
                });
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Imports new journals from the past three days that have not yet been
        /// added to the Pandacap inbox.
        /// </summary>
        /// <returns></returns>
        public async Task ImportJournalsByUsersWeWatchAsync()
        {
            if (await weasylClientFactory.CreateWeasylClientAsync() is not WeasylClient weasylClient)
                return;

            DateTimeOffset someTimeAgo = DateTimeOffset.UtcNow.AddDays(-3);

            var existingPosts = await context.InboxWeasylJournals
                .Where(item => item.PostedAt >= someTimeAgo)
                .ToListAsync();

            Dictionary<string, WeasylClient.AvatarResponse> avatars = [];

            var journals = await weasylClient.ExtractJournalsAsync();

            foreach (var journal in journals)
            {
                if (journal.time < someTimeAgo)
                    break;

                if (existingPosts.Any(p => p.Url == journal.post.href))
                    continue;

                var avatar = await weasylClient.GetAvatarAsync(journal.user.name);

                context.InboxWeasylJournals.Add(new()
                {
                    Avatar = avatar?.avatar,
                    Id = Guid.NewGuid(),
                    PostedAt = journal.time,
                    ProfileUrl = journal.user.href,
                    Title = journal.post.name,
                    Url = journal.post.href,
                    Username = journal.user.name
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
