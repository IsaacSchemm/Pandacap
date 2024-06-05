using Pandacap.Data;

namespace Pandacap.Models
{
    public class ProfileViewModel
    {
        public IEnumerable<DeviantArtArtworkDeviation> RecentArtwork { get; set; } = [];
        public IEnumerable<DeviantArtTextDeviation> RecentTextPosts { get; set; } = [];

        public int FollowerCount { get; set; }
        public int FollowingCount { get; set; }

        public IDeviation? MostRecentPost => Enumerable.Empty<IDeviation>()
            .Concat(RecentArtwork)
            .Concat(RecentTextPosts)
            .OrderByDescending(x => x.PublishedTime)
            .FirstOrDefault();
    }
}
