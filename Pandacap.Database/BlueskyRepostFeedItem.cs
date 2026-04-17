namespace Pandacap.Database
{
    public class BlueskyRepostFeedItem : BlueskyShareFeedItem
    {
        public User RepostedBy { get; set; } = new();

        public DateTimeOffset RepostedAt { get; set; } = new();

        public override User SharedBy => RepostedBy;

        public override DateTimeOffset SharedAt => RepostedAt;
    }
}
