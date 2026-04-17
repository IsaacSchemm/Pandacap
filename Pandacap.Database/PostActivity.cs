namespace Pandacap.Database
{
    public class PostActivity
    {
        public string Id { get; set; } = "";
        public string InReplyTo { get; set; } = "";
        public string ActorId { get; set; } = "";
        public string ActivityType { get; set; } = "";
        public DateTimeOffset AddedAt { get; set; }
    }
}
