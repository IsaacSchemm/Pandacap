using Pandacap.UI.Badges;
using Pandacap.UI.Elements;

namespace Pandacap.Database
{
    /// <summary>
    /// An ActivityPub actor who this Pandacap actor is following.
    /// </summary>
    public class Follow : RemoteActorRelationship, IFollow
    {
        /// <summary>
        /// A random ID used in the ActivityPub ID of the Follow activity that was sent.
        /// Used to send an Undo when unfollowing the actor.
        /// </summary>
        public Guid FollowGuid { get; set; }

        /// <summary>
        /// Whether the remote actor has accepted the follow request.
        /// </summary>
        public bool Accepted { get; set; }

        /// <summary>
        /// Whether to ignore images from this actor's posts when adding them to the inbox (sending them to the Text Posts section).
        /// </summary>
        public bool IgnoreImages { get; set; }

        /// <summary>
        /// Whether to include this actor's reposts/boosts if they contain images.
        /// </summary>
        public bool IncludeImageShares { get; set; }

        /// <summary>
        /// Whether to include this actor's reposts/boosts if they do not contain images.
        /// </summary>
        public bool IncludeTextShares { get; set; }

        Badge IFollow.Badge => Badges.ActivityPub;

        string? IFollow.LinkUrl => Url ?? ActorId;

        string IFollow.Username => PreferredUsername ?? ActorId;

        bool IFollow.Filtered => !IncludeImageShares || !IncludeTextShares;
    }
}
