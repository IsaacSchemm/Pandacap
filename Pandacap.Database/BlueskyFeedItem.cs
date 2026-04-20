using Pandacap.Text;
using Pandacap.UI.Badges;
using Pandacap.UI.Elements;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pandacap.Database
{
    public abstract class BlueskyFeedItem : IInboxPost
    {
        [Key]
        public string CID { get; set; } = "";

        public DateTimeOffset CreatedAt { get; set; }

        public List<string> Labels { get; set; } = [];

        public string Text { get; set; } = "";

        public class Image
        {
            public string CID { get; set; } = "";
            public string Alt { get; set; } = "";
        }

        public List<Image> Images { get; set; } = [];

        public DateTimeOffset? DismissedAt { get; set; }

        [NotMapped]
        public bool AdultContent => Labels
            .Intersect(["porn", "sexual", "nudity", "sexual-figurative", "graphic-media"])
            .Any();

        public class User
        {
            public string DID { get; set; } = "";
            public string PDS { get; set; } = "";
            public string? DisplayName { get; set; }
            public string? Handle { get; set; } = "";
            public string? AvatarCID { get; set; }
        }

        [NotMapped]
        public abstract string OriginalDID { get; }

        [NotMapped]
        public abstract string OriginalPDS { get; }

        [NotMapped]
        public abstract string OriginalRecordKey { get; }

        [NotMapped]
        public abstract User AttributeTo { get; }

        [NotMapped]
        public abstract DateTimeOffset DateTo { get; }

        [NotMapped]
        public abstract bool IsShare { get; }

        bool IInboxPost.IsPodcast => false;

        Badge IPost.Badge => Badges.ATProto.WithText(OriginalPDS);

        string IPost.DisplayTitle => ExcerptGenerator.FromText(60, Text);

        string IPost.Id => $"{CID}";

        string IPost.InternalUrl => $"/ATProto/ViewBlueskyPost?did={OriginalDID}&rkey={OriginalRecordKey}";

        string IPost.ExternalUrl => $"https://bsky.app/profile/{OriginalDID}/post/{OriginalRecordKey}";

        DateTimeOffset IPost.PostedAt => DateTo;

        string? IPost.ProfileUrl => $"https://bsky.app/profile/{AttributeTo.DID}";

        IEnumerable<IPostThumbnail> IPost.Thumbnails => Images.Select(image => new PostThumbnailAdapter(OriginalDID, image));

        string? IPost.Username => AttributeTo.Handle ?? AttributeTo.DID;

        string? IPost.Usericon => AttributeTo.AvatarCID is string cid
            ? $"/ATProto/GetBlob?did={AttributeTo.DID}&cid={cid}"
            : null;

        private class PostThumbnailAdapter(string did, Image image) : IPostThumbnail
        {
            string IPostThumbnail.Url => $"/ATProto/GetBlob?did={did}&cid={image.CID}";

            string IPostThumbnail.AltText => image.Alt;
        }
    }
}
