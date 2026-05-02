using Pandacap.Database;
using Pandacap.PlatformLinks.Interfaces;

namespace Pandacap.Models
{
    public class ProfileViewModel : IProfileHeadingModel
    {
        public IReadOnlyList<IPlatformLink> PlatformLinks { get; set; } = [];

        public IReadOnlyList<Post> RecentArtwork { get; set; } = [];
        public IReadOnlyList<Post> RecentTextPosts { get; set; } = [];

        public int FollowerCount { get; set; }
        public int FollowingCount { get; set; }
        public int FavoritesCount { get; set; }

        public bool VectorSearchEnabled { get; set; }
    }
}
