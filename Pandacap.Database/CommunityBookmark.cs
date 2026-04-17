using System.ComponentModel.DataAnnotations.Schema;

namespace Pandacap.Database
{
    /// <summary>
    /// A bookmarked Lemmy community.
    /// (Pandacap is not designed to follow Lemmy communities directly, but it can interact with them through ActivityPub and through Lemmy's public API.)
    /// </summary>
    public class CommunityBookmark : RemoteActorRelationship
    {
        [NotMapped]
        public Uri Uri => new(ActorId);

        [NotMapped]
        public string Host => Uri.Host;

        [NotMapped]
        public string? Name => PreferredUsername;
    }
}
