﻿namespace Pandacap.Data
{
    public class UserTextDeviation : IUserDeviation, IPost
    {
        public Guid Id { get; set; }
        public string? LinkUrl { get; set; }
        public string? Title { get; set; }
        public string? Username { get; set; }
        public string? Usericon { get; set; }
        public DateTimeOffset PublishedTime { get; set; }
        public bool IsMature { get; set; }

        public string? Description { get; set; }
        public List<string> Tags { get; set; } = []; 
        
        public string? Excerpt { get; set; }

        string IPost.Id => $"{Id}";

        string? IPost.DisplayTitle
        {
            get
            {
                string? excerpt = this.Excerpt;
                if (excerpt != null && excerpt.Length > 60)
                    excerpt = excerpt[..60] + "...";

                return Title ?? excerpt ?? $"{Id}";
            }
        }

        DateTimeOffset IPost.Timestamp => PublishedTime;

        DateTimeOffset? IPost.DismissedAt => DateTimeOffset.MinValue;

        IEnumerable<IThumbnail> IPost.Thumbnails => [];

        IEnumerable<string> IUserDeviation.Tags => Tags;
    }
}
