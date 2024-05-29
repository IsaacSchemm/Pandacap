namespace Pandacap.Data
{
    public abstract class ActivityPubInboxPost : IInboxPost
    {
        public string Id { get; set; } = "";

        public string CreatedBy { get; set; } = "";

        public string? Username { get; set; }
        public string? Usericon { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string? Summary { get; set; }
        public bool Sensitive { get; set; }

        public string? Name { get; set; }

        public DateTimeOffset? DismissedAt { get; set; }

        public string? Content { get; set; }

        string? IInboxPost.DisplayTitle => Name ?? Timestamp.UtcDateTime.ToString();

        string? IInboxPost.LinkUrl => Id;
    }
}
