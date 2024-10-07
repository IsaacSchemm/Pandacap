using Pandacap.Data;

namespace Pandacap.Models
{
    public class ProfileViewModel
    {
        public IEnumerable<string> BlueskyDIDs { get; set; } = [];
        public IEnumerable<string> WeasylUsernames { get; set; } = [];

        public IEnumerable<UserPost> RecentArtwork { get; set; } = [];
        public IEnumerable<UserPost> RecentTextPosts { get; set; } = [];

        public int FollowerCount { get; set; }
        public int FollowingCount { get; set; }
        public int FavoritesCount { get; set; }

        public UserPost? MostRecentPost => Enumerable.Empty<UserPost>()
            .Concat(RecentArtwork)
            .Concat(RecentTextPosts)
            .OrderByDescending(x => x.PublishedTime)
            .FirstOrDefault();
    }
}
