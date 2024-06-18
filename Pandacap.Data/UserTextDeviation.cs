namespace Pandacap.Data
{
    public class UserTextDeviation : IUserPost, IPost
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

        IUserPostImage? IUserPost.Image => null;

        DateTimeOffset IUserPost.Timestamp => PublishedTime;

        bool IUserPost.HideTitle => !FederateTitle;

        bool IUserPost.IsArticle => FederateTitle;

        IEnumerable<string> IUserPost.Tags => Tags;

        string? IPost.Username => null;

        string? IPost.Usericon => null;
    }
}
