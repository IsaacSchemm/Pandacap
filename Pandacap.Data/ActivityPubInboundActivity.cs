using System.ComponentModel.DataAnnotations;

namespace Pandacap.Data
{
    /// <summary>
    /// Another ActivityPub actor's interaction with a Pandacap post.
    /// </summary>
    public class ActivityPubInboundActivity
    {
        /// <summary>
        /// An internal ID.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// The ID of the activity.
        /// </summary>
        [Required]
        public string ActivityId { get; set; } = "";

        /// <summary>
        /// The type of the activity (e.g. Like, Announce).
        /// </summary>
        [Required]
        public string ActivityType { get; set; } = "";

        /// <summary>
        /// The ID of the post that was interacted with.
        /// </summary>
        public Guid DeviationId { get; set; }

        /// <summary>
        /// The date/time at which this interaction was added.
        /// </summary>
        public DateTimeOffset AddedAt { get; set; }

        /// <summary>
        /// The ID of the actor who interacted with the post.
        /// </summary>
        [Required]
        public string ActorId { get; set; } = "";
    }
}
