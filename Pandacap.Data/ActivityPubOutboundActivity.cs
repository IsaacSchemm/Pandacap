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
        /// The pre-serialized JSON-LD body of the activity.
        /// </summary>
        [Required]
        public string JsonBody { get; set; } = "{}";

        /// <summary>
        /// The DeviantArt ID of the post that this activity is intended to
        /// create, update, or delete (if any).
        /// </summary>
        public Guid? DeviationId { get; set; }

        /// <summary>
        /// When this activity was added to Pandacap's database.
        /// </summary>
        public DateTimeOffset StoredAt { get; set; }

        /// <summary>
        /// Whether to prevent this activity from being fetched from the
        /// Pandacap server using its ID.
        /// </summary>
        public bool Unresolvable { get; set; }

        /// <summary>
        /// Whether this activity should be omitted from the outbox. This
        /// should be set to true if the post it's intended to create or update
        /// has since been deleted.
        /// </summary>
        public bool HideFromOutbox { get; set; }
    }
}
