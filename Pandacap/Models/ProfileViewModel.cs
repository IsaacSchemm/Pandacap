﻿using Pandacap.Data;

namespace Pandacap.Models
{
    public class ProfileViewModel
    {
        public IEnumerable<ProfileProperty> ProfileProperties { get; set; } = [];

        public IEnumerable<UserPost> RecentArtwork { get; set; } = [];
        public IEnumerable<UserPost> RecentTextPosts { get; set; } = [];
        public IEnumerable<ActivityInfo> RecentActivities { get; set; } = [];

        public int FollowerCount { get; set; }
        public int FollowingCount { get; set; }

        public bool BridgyFed { get; set; }

        public UserPost? MostRecentPost => Enumerable.Empty<UserPost>()
            .Concat(RecentArtwork)
            .Concat(RecentTextPosts)
            .OrderByDescending(x => x.PublishedTime)
            .FirstOrDefault();
    }
}
