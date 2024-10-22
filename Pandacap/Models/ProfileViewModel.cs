using Pandacap.Data;

namespace Pandacap.Models
{
    public class ProfileViewModel
    {
        public IEnumerable<string> BlueskyDIDs { get; set; } = [];
        public IEnumerable<string> DeviantArtUsernames { get; set; } = [];
        public IEnumerable<string> WeasylUsernames { get; set; } = [];

        public IEnumerable<Post> RecentArtwork { get; set; } = [];
        public IEnumerable<Post> RecentJournalEntries { get; set; } = [];
        public IEnumerable<Post> RecentStatusUpdates { get; set; } = [];

        public int FollowerCount { get; set; }
        public int FollowingCount { get; set; }
        public int FavoritesCount { get; set; }
        public int CommunityBookmarksCount { get; set; }

        public Post? MostRecentPost => Enumerable.Empty<Post>()
            .Concat(RecentArtwork)
            .Concat(RecentJournalEntries)
            .Concat(RecentStatusUpdates)
            .OrderByDescending(x => x.PublishedTime)
            .FirstOrDefault();
    }
}
