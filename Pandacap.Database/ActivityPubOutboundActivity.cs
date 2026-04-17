namespace Pandacap.Database
{
    /// <summary>
    /// An ActivityPub activity that is queued to be sent to a remote actor.
    /// </summary>
    public class ActivityPubOutboundActivity
    {
        public Guid Id { get; set; }

        public string Inbox { get; set; } = "";

        public string JsonBody { get; set; } = "{}";

        public DateTimeOffset StoredAt { get; set; }

        public DateTimeOffset DelayUntil { get; set; }
    }
}
