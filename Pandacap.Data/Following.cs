using System.ComponentModel.DataAnnotations;

namespace Pandacap.Data
{
    /// <summary>
    /// An ActivityPub actor who this Pandacap actor is following.
    /// </summary>
    public class Following
    {
        /// <summary>
        /// An internal ID for this follow.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The date/time at which this follow was added.
        /// </summary>
        public DateTimeOffset AddedAt { get; set; }

        /// <summary>
        /// The follower's actor ID.
        /// </summary>
        public string ActorId { get; set; } = "";

        /// <summary>
        /// The ID of the Follow activity sent to the remote server.
        /// </summary>
        [Required]
        public string FollowId { get; set; } = "";

        /// <summary>
        /// Whether the follow has been accepted.
        /// </summary>
        public bool Accepted { get; set; }

        /// <summary>
        /// This actor's personal ActivityPub inbox.
        /// </summary>
        [Required]
        public string Inbox { get; set; } = "";

        /// <summary>
        /// The shared inbox of this actor's ActivityPub server, if any.
        /// </summary>
        public string? SharedInbox { get; set; }
    }
}
