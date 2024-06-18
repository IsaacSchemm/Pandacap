namespace Pandacap.Data
{
    public class UserTextDeviation : IPost
    {
        public Guid Id { get; set; }
        public string? LinkUrl { get; set; }
        public string? Title { get; set; }
        public bool FederateTitle { get; set; }
        public DateTimeOffset PublishedTime { get; set; }
        public bool IsMature { get; set; }

        public string? Description { get; set; }
        public List<string> Tags { get; set; } = [];

        string IPost.Id => $"{Id}";

        string IPost.DisplayTitle => Title ?? $"{Id}";

        DateTimeOffset IPost.Timestamp => PublishedTime;

        IEnumerable<IThumbnail> IPost.Thumbnails => [];

        string? IPost.Username => null;

        string? IPost.Usericon => null;
    }
}
