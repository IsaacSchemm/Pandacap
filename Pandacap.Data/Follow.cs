using System.ComponentModel.DataAnnotations;

namespace Pandacap.Data
{
    /// <summary>
    /// An ActivityPub actor who this Pandacap actor is following.
    /// </summary>
    public class Follow : IRemoteActorRelationship
    {
        /// <summary>
        /// The follower's actor ID.
        /// </summary>
        [Key]
        public string ActorId { get; set; } = "";

        /// <summary>
        /// The date/time at which this follow was added.
        /// </summary>
        public DateTimeOffset AddedAt { get; set; }

        /// <summary>
        /// The Pandacap-generated ID used for this activity when it was placed
        /// in the ActivityPubOutboundActivities table.
        /// </summary>
        [Required]
        public Guid FollowGuid { get; set; }

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

        bool IRemoteActorRelationship.Pending => !Accepted;
    }
}
