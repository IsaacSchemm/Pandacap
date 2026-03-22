using System.ComponentModel.DataAnnotations;

namespace Pandacap.Database
{
    /// <summary>
    /// A relationship of some sort between Pandacap and a remote ActivityPub actor.
    /// </summary>
    public abstract class RemoteActorRelationship
    {
        /// <summary>
        /// The actor's ActivityPub ID / URL.
        /// </summary>
        [Key]
        public string ActorId { get; set; } = "";

        /// <summary>
        /// The date/time when the Pandacap admin added this actor to the relevant list.
        /// </summary>
        public DateTimeOffset AddedAt { get; set; }

        /// <summary>
        /// The URL of the actor's ActivityPub inbox.
        /// </summary>
        public string Inbox { get; set; } = "";

        /// <summary>
        /// The URL of the actor's ActivityPub instance's shared inbox, if any.
        /// </summary>
        public string? SharedInbox { get; set; }

        /// <summary>
        /// The actor's username, if any.
        /// </summary>
        public string? PreferredUsername { get; set; }

        /// <summary>
        /// The URL of the actor's avatar, if any.
        /// </summary>
        public string? IconUrl { get; set; }

        /// <summary>
        /// The URL of the actor's profile page, if any.
        /// </summary>
        public string? Url { get; set; }
    }
}
