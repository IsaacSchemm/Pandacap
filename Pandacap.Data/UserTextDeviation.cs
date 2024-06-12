namespace Pandacap.Data
{
    public class UserTextDeviation : IUserDeviation, IPost
    {
        public Guid Id { get; set; }
        public string? LinkUrl { get; set; }
        public string? Title { get; set; }
        public bool FederateTitle { get; set; }
        public DateTimeOffset PublishedTime { get; set; }
        public bool IsMature { get; set; }

        public string? Description { get; set; }
        public List<string> Tags { get; set; } = [];
        
        public string? Excerpt { get; set; }

        string IPost.Id => $"{Id}";

        string IPost.DisplayTitle => Title ?? $"{Id}";

        DateTimeOffset IPost.Timestamp => PublishedTime;

        IEnumerable<IThumbnail> IPost.Thumbnails => [];

        IEnumerable<string> IUserDeviation.Tags => Tags;

        string? IPost.Username => null;

        string? IPost.Usericon => null;
    }
}
