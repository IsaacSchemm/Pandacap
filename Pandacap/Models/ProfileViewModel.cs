using Pandacap.Data;

namespace Pandacap.Models
{
    public class ProfileViewModel
    {
        public IEnumerable<UserArtworkDeviation> RecentArtwork { get; set; } = [];
        public IEnumerable<UserTextDeviation> RecentTextPosts { get; set; } = [];
        public IEnumerable<RemoteActivity> RecentActivities { get; set; } = [];

        public int FollowerCount { get; set; }
        public int FollowingCount { get; set; }

        public IUserDeviation? MostRecentPost => Enumerable.Empty<IUserDeviation>()
            .Concat(RecentArtwork)
            .Concat(RecentTextPosts)
            .OrderByDescending(x => x.PublishedTime)
            .FirstOrDefault();
    }
}
