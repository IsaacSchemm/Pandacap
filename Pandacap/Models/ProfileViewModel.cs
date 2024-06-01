using Pandacap.Data;

namespace Pandacap.Models
{
    public class ProfileViewModel
    {
        public IEnumerable<DeviantArtArtworkDeviation> RecentArtwork { get; set; } = [];
        public IEnumerable<DeviantArtTextDeviation> RecentTextPosts { get; set; } = [];

        public int FollowerCount { get; set; }
        public int FollowingCount { get; set; }

        public DeviantArtDeviation? MostRecentPost => Enumerable.Empty<DeviantArtDeviation>()
            .Concat(RecentArtwork)
            .Concat(RecentTextPosts)
            .OrderByDescending(x => x.PublishedTime)
            .FirstOrDefault();
    }
}
