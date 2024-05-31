using System.ComponentModel.DataAnnotations;

namespace Pandacap.Data
{
    /// <summary>
    /// A copy of an ActivityPub activity that had been, or will be, sent to a
    /// specific inbox.
    /// </summary>
    public class ActivityPubOutboundActivityRecipient
    {
        /// <summary>
        /// An internal ID for this copy of the activity.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The ID of the ActivityPubOutboundActivity to send.
        /// </summary>
        public Guid ActivityId { get; set; }

        /// <summary>
        /// The inbox URL to send to.
        /// </summary>
        [Required]
        public string Inbox { get; set; } = "";

        /// <summary>
        /// When this copy of the activity was added to Pandacap's database.
        /// Copies of activities will be removed once they are sent, or when
        /// they have been waiting for a long period of time without a
        /// succcessful send.
        /// </summary>
        public DateTimeOffset StoredAt { get; set; }

        /// <summary>
        /// If this date/time is in the future, this activity (and any further
        /// activities to the same inbox) should not be sent to this inbox
        /// until at least the next run.
        /// </summary>
        public DateTimeOffset DelayUntil { get; set; }
    }
}
