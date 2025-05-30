using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.LowLevel.MyLinks;

namespace Pandacap.Models
{
    public class ProfileViewModel
    {
        public IEnumerable<BlueskyProfileResolver.ProfileInformation> BlueskyFavoriteProfiles { get; set; } = [];

        public IEnumerable<MyLink> MyLinks { get; set; } = [];

        public IEnumerable<Post> RecentArtwork { get; set; } = [];
        public IEnumerable<IPost> RecentFavorites { get; set; } = [];

        public IEnumerable<Post> RecentTextPosts { get; set; } = [];

        public int FollowerCount { get; set; }
        public int FollowingCount { get; set; }
        public int FavoritesCount { get; set; }
        public int CommunityBookmarksCount { get; set; }

        public Post? MostRecentPost => Enumerable.Empty<Post>()
            .Concat(RecentArtwork)
            .Concat(RecentTextPosts)
            .OrderByDescending(x => x.PublishedTime)
            .FirstOrDefault();
    }
}
