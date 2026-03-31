using Pandacap.UI.Badges;
using Pandacap.UI.Elements;
using System.ComponentModel.DataAnnotations;

namespace Pandacap.Database
{
    /// <summary>
    /// An atproto user that the Pandacap administrator follows.
    /// </summary>
    public class ATProtoFeed : IFollow
    {
        /// <summary>
        /// The user's DID.
        /// </summary>
        [Key]
        public string DID { get; set; } = "";

        /// <summary>
        /// The user's last known PDS.
        /// </summary>
        public string CurrentPDS { get; set; } = "";

        /// <summary>
        /// Whether to add this users' posts to the inbox even if they don't have images.
        /// </summary>
        public bool IncludePostsWithoutImages { get; set; } = true;

        /// <summary>
        /// Whether to add this users' posts to the inbox which are replies to other users' posts.
        /// </summary>
        public bool IncludeReplies { get; set; } = false;

        /// <summary>
        /// Whether to add this users' quote posts to the inbox.
        /// </summary>
        public bool IncludeQuotePosts { get; set; } = true;

        /// <summary>
        /// Whether to ignore images from this users' posts when adding them to the inbox (sending them to the Text Posts section).
        /// </summary>
        public bool IgnoreImages { get; set; } = false;

        /// <summary>
        /// The user's last known handle, if any.
        /// </summary>
        public string? Handle { get; set; }

        /// <summary>
        /// The user's last known display name, if any.
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// The CID of the user's last known avatar, if any.
        /// </summary>
        public string? AvatarCID { get; set; }

        /// <summary>
        /// A list of NSIDs for atproto objects to look for (such as app.bsky.feed.post or app.bsky.feed.repost).
        /// </summary>
        public List<string> NSIDs { get; set; } = [];

        /// <summary>
        /// The CID of the most recent commit to the atproto repo when Pandacap last checked it.
        /// </summary>
        public string? LastCommitCID { get; set; }

        /// <summary>
        /// Some of the most recent CIDs seen in the repo when Pandacap last checked it.
        /// Used to tell Panadcap when to stop looking through the repo's objects on the next run.
        /// </summary>
        public List<string> LastCIDsSeen { get; set; } = [];

        Badge IFollow.Badge => Badges.ATProto;

        string? IFollow.LinkUrl => $"https://bsky.app/profile/{Handle}";

        string IFollow.Username => DisplayName ?? Handle ?? DID;

        string? IFollow.IconUrl => AvatarCID == null
            ? null
            : $"/ATProto/GetBlob?did={DID}&cid={AvatarCID}";

        bool IFollow.Filtered =>
            !IncludePostsWithoutImages
            || !IncludeQuotePosts
            || !NSIDs.Contains("app.bsky.feed.post")
            || !NSIDs.Contains("app.bsky.feed.repost");
    }
}
