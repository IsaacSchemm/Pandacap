using System.ComponentModel.DataAnnotations;

namespace Pandacap.Data
{
    /// <summary>
    /// An ActivityPub actor who is following this Pandacap actor.
    /// </summary>
    public class Follower : IRemoteActorRelationship
    {
        /// <summary>
        /// The follower's actor ID.
        /// </summary>
        [Key]
        public string ActorId { get; set; } = "";

        /// <summary>
        /// The date/time at which this follower was added.
        /// </summary>
        public DateTimeOffset AddedAt { get; set; }

        /// <summary>
        /// This actor's personal ActivityPub inbox.
        /// </summary>
        [Required]
        public string Inbox { get; set; } = "";

        /// <summary>
        /// The shared inbox of this actor's ActivityPub server, if any.
        /// </summary>
        public string? SharedInbox { get; set; }

        /// <summary>
        /// The preferred username of this actor, if any.
        /// </summary>
        public string? PreferredUsername { get; set; }

        /// <summary>
        /// The icon URL of this actor, if any.
        /// </summary>
        public string? IconUrl { get; set; }

        bool IRemoteActorRelationship.Pending => false;
    }
}
