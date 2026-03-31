namespace Pandacap.Database
{
    public class BlueskyLikeFeedItem : BlueskyShareFeedItem
    {
        public User LikedBy { get; set; } = new();

        public DateTimeOffset LikedAt { get; set; } = new();

        public override User SharedBy => LikedBy;

        public override DateTimeOffset SharedAt => LikedAt;
    }
}
