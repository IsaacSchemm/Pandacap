using Pandacap.Data;

namespace Pandacap.Models
{
    public class ProfileViewModel
    {
        public IEnumerable<ProfileProperty> ProfileProperties { get; set; } = [];

        public IEnumerable<UserArtworkDeviation> RecentArtwork { get; set; } = [];
        public IEnumerable<UserTextDeviation> RecentTextPosts { get; set; } = [];
        public IEnumerable<ActivityInfo> RecentActivities { get; set; } = [];

        public int FollowerCount { get; set; }
        public int FollowingCount { get; set; }

        public bool BridgyFed { get; set; }

        public IUserPost? MostRecentPost => Enumerable.Empty<IUserPost>()
            .Concat(RecentArtwork)
            .Concat(RecentTextPosts)
            .OrderByDescending(x => x.Timestamp)
            .FirstOrDefault();
    }
}
