using System.ComponentModel.DataAnnotations;

namespace Pandacap.Data
{
    /// <summary>
    /// An ActivityPub activity that can be sent to a remote actor and/or
    /// included in the outbox.
    /// </summary>
    public class ActivityPubOutboundActivity
    {
        /// <summary>
        /// A Pandacap-generated ID for this activity.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The inbox ID / URL to send to.
        /// </summary>
        [Required]
        public string Inbox { get; set; } = "";

        /// <summary>
        /// The pre-serialized JSON-LD body of the activity.
        /// </summary>
        [Required]
        public string JsonBody { get; set; } = "{}";

        /// <summary>
        /// When this activity was added to Pandacap's database.
        /// </summary>
        public DateTimeOffset StoredAt { get; set; }

        /// <summary>
        /// If this date/time is in the future, this activity (and any further
        /// activities to the same inbox) should be delayed until at least the
        /// next run.
        /// </summary>
        public DateTimeOffset DelayUntil { get; set; }
    }
}
