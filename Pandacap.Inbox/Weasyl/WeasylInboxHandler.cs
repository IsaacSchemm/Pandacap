using Microsoft.EntityFrameworkCore;
using Pandacap.Credentials.Interfaces;
using Pandacap.Database;
using Pandacap.Inbox.Interfaces;
using Pandacap.Weasyl.Interfaces;

namespace Pandacap.Inbox.Weasyl
{
    public class WeasylInboxHandler(
        PandacapDbContext pandacapDbContext,
        IUserAwareWeasylClientFactory userAwareWeasylClientFactory) : IInboxSource
    {
        /// <summary>
        /// Imports new posts from the past three days that have not yet been
        /// added to the Pandacap inbox.
        /// </summary>
        /// <returns></returns>
        internal async Task ImportSubmissionsByUsersWeWatchAsync(CancellationToken cancellationToken)
        {
            if (await userAwareWeasylClientFactory.CreateWeasylClientAsync(cancellationToken) is not IWeasylClient weasylClient)
                return;

            DateTimeOffset someTimeAgo = DateTimeOffset.UtcNow.AddDays(-3);

            var existingPosts = await pandacapDbContext.InboxWeasylSubmissions
                .Where(item => item.PostedAt >= someTimeAgo)
                .ToListAsync(cancellationToken);

            Dictionary<string, Pandacap.Weasyl.Models.WeasylApi.AvatarResponse> avatars = [];

            await foreach (var submission in weasylClient.GetMessagesSubmissionsAsync(CancellationToken.None))
            {
                if (submission.posted_at < someTimeAgo)
                    break;

                if (existingPosts.Any(e => e.Submitid == submission.submitid))
                    continue;

                if (!avatars.TryGetValue(submission.owner_login, out var avatarResponse))
                {
                    avatarResponse = await weasylClient.GetAvatarAsync(
                        submission.owner_login,
                        CancellationToken.None);

                    avatars[submission.owner_login] = avatarResponse;
                }

                pandacapDbContext.InboxWeasylSubmissions.Add(new()
                {
                    Id = Guid.NewGuid(),
                    Submitid = submission.submitid,
                    Title = submission.title,
                    Rating = submission.rating,
                    PostedBy = new InboxWeasylSubmission.User
                    {
                        Login = submission.owner_login,
                        DisplayName = submission.owner,
                        Avatar = avatarResponse.avatar
                    },
                    PostedAt = submission.posted_at,
                    Thumbnails = [.. submission.media.thumbnail
                        .Select(t => new InboxWeasylSubmission.Image
                        {
                            Url = t.url
                        })],
                    Url = submission.link
                });
            }

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Imports new journals from the past three days that have not yet been
        /// added to the Pandacap inbox.
        /// </summary>
        /// <returns></returns>
        public async Task ImportJournalsByUsersWeWatchAsync(CancellationToken cancellationToken)
        {
            if (await userAwareWeasylClientFactory.CreateWeasylClientAsync(cancellationToken) is not IWeasylClient weasylClient)
                return;

            DateTimeOffset someTimeAgo = DateTimeOffset.UtcNow.AddDays(-3);

            var existingPosts = await pandacapDbContext.InboxWeasylJournals
                .Where(item => item.PostedAt >= someTimeAgo)
                .ToListAsync(cancellationToken);

            Dictionary<string, Pandacap.Weasyl.Models.WeasylApi.AvatarResponse> avatars = [];

            var journals = await weasylClient.ExtractJournalsAsync(CancellationToken.None);

            foreach (var journal in journals)
            {
                if (journal.time < someTimeAgo)
                    break;

                if (existingPosts.Any(p => p.Url == journal.post.href))
                    continue;

                var avatar = await weasylClient.GetAvatarAsync(
                    journal.user.name,
                    CancellationToken.None);

                pandacapDbContext.InboxWeasylJournals.Add(new()
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

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }

        async Task IInboxSource.ImportNewPostsAsync(CancellationToken cancellationToken)
        {
            await ImportSubmissionsByUsersWeWatchAsync(cancellationToken);
            await ImportJournalsByUsersWeWatchAsync(cancellationToken);
        }
    }
}
