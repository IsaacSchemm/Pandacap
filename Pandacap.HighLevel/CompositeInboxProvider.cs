using Microsoft.EntityFrameworkCore;
using Pandacap.Data;

namespace Pandacap.HighLevel
{
    public class CompositeInboxProvider(
        PandacapDbContext context)
    {
        public IAsyncEnumerable<IInboxPost> GetAllInboxPostsAsync()
        {
            var activityPub = context.InboxActivityStreamsPosts
                .OrderByDescending(d => d.PostedAt)
                .AsAsyncEnumerable()
                .OfType<IInboxPost>();

            var atproto = context.ATProtoInboxItems
                .OrderByDescending(d => d.Timestamp)
                .AsAsyncEnumerable()
                .OfType<IInboxPost>();

            var bluesky1 = context.BlueskyPostFeedItems
                .OrderByDescending(d => d.CreatedAt)
                .AsAsyncEnumerable()
                .OfType<IInboxPost>();

            var bluesky2 = context.BlueskyRepostFeedItems
                .OrderByDescending(d => d.RepostedAt)
                .AsAsyncEnumerable()
                .OfType<IInboxPost>();

            var bluesky3 = context.BlueskyLikeFeedItems
                .OrderByDescending(d => d.LikedAt)
                .AsAsyncEnumerable()
                .OfType<IInboxPost>();

            var deviantArtImages = context.InboxArtworkDeviations
                .OrderByDescending(d => d.Timestamp)
                .AsAsyncEnumerable()
                .OfType<IInboxPost>();

            var deviantArtText = context.InboxTextDeviations
                .OrderByDescending(d => d.Timestamp)
                .AsAsyncEnumerable()
                .OfType<IInboxPost>();

            var furAffinitySubmissions = context.InboxFurAffinitySubmissions
                .OrderByDescending(d => d.PostedAt)
                .AsAsyncEnumerable()
                .OfType<IInboxPost>();

            var furAffinityJournals = context.InboxFurAffinityJournals
                .OrderByDescending(d => d.PostedAt)
                .AsAsyncEnumerable()
                .OfType<IInboxPost>();

            var generalItems = context.GeneralInboxItems
                .OrderByDescending(d => d.Timestamp)
                .AsAsyncEnumerable()
                .OfType<IInboxPost>();

            var weasylSubmissions = context.InboxWeasylSubmissions
                .OrderByDescending(d => d.PostedAt)
                .AsAsyncEnumerable()
                .OfType<IInboxPost>();

            var weasylJournals = context.InboxWeasylJournals
                .OrderByDescending(d => d.PostedAt)
                .AsAsyncEnumerable()
                .OfType<IInboxPost>();

            return
                new[]
                {
                    activityPub,
                    atproto,
                    bluesky1,
                    bluesky2,
                    bluesky3,
                    deviantArtImages,
                    deviantArtText,
                    furAffinitySubmissions,
                    furAffinityJournals,
                    generalItems,
                    weasylSubmissions,
                    weasylJournals
                }
                .MergeNewest(post => post.PostedAt)
                .Where(post => post.DismissedAt == null);
        }
    }
}
