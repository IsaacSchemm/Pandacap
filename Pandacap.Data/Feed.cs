namespace Pandacap.Data
{
    public class Feed
    {
        public Guid Id { get; set; }

        public string FeedUrl { get; set; } = "";

        public string? FeedTitle { get; set; }

        public string? FeedWebsiteUrl { get; set; }

        public string? FeedIconUrl { get; set; }

        public DateTimeOffset LastCheckedAt { get; set; } = DateTimeOffset.MinValue;
    }
}
