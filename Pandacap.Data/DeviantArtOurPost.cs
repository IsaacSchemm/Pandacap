namespace Pandacap.Data
{
    public class DeviantArtOurImage
    {
        public string Url { get; set; } = "";
        public string ContentType { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public abstract class DeviantArtOurPost : IPost
    {
        public Guid Id { get; set; }
        public string? Url { get; set; }
        public string? Title { get; set; }
        public string? Username { get; set; }
        public string? Usericon { get; set; }
        public DateTimeOffset PublishedTime { get; set; }
        public bool IsMature { get; set; }

        public string? Description { get; set; }
        public List<string> Tags { get; set; } = [];

        string IPost.Id => $"{Id}";

        string? IPost.DisplayTitle
        {
            get
            {
                string? excerpt = (this as DeviantArtOurTextPost)?.Excerpt;
                if (excerpt != null && excerpt.Length > 40)
                    excerpt = excerpt[..40] + "...";

                return Title ?? excerpt ?? $"{Id}";
            }
        }

        DateTimeOffset IPost.Timestamp => PublishedTime;

        string? IPost.LinkUrl => Url;

        DateTimeOffset? IPost.DismissedAt => null;
    }
}
