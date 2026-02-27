using Pandacap.Data;
using Pandacap.HighLevel.PlatformLinks;
using Pandacap.LowLevel.MyLinks;

namespace Pandacap.Models
{
    public class ProfileViewModel
    {
        public IReadOnlyList<IPlatformLink> PlatformLinks { get; set; } = [];

        public IReadOnlyList<MyLink> MyLinks { get; set; } = [];

        public IReadOnlyList<Post> RecentArtwork { get; set; } = [];
        public IReadOnlyList<IPost> RecentFavorites { get; set; } = [];

        public IReadOnlyList<Post> RecentTextPosts { get; set; } = [];
        public IReadOnlyList<Post> RecentLinks { get; set; } = [];

        public int FollowerCount { get; set; }
        public int FollowingCount { get; set; }
        public int FavoritesCount { get; set; }
        public int CommunityBookmarksCount { get; set; }
    }
}
