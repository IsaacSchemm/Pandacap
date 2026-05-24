using Pandacap.UI.Badges;
using Pandacap.UI.Elements;
using System.ComponentModel.DataAnnotations;

namespace Pandacap.Database
{
    public class StandardSiteDocumentFeedItem : IInboxPost
    {
        [Key]
        public string CID { get; set; } = "";

        public DateTimeOffset PublishedAt { get; set; }

        public DateTimeOffset AddedAt { get; set; }

        public string Title { get; set; } = "";

        public DateTimeOffset? DismissedAt { get; set; }

        public string RecordKey { get; set; } = "";

        public BlueskyFeedItem.User Author { get; set; } = new();

        bool IInboxPost.IsPodcast => false;

        bool IInboxPost.IsShare => false;

        Badge IPost.Badge => Badges.ATProto.WithText(Author.PDS);

        string IPost.DisplayTitle => Title;

        string IPost.Id => $"{CID}";

        string? IPost.InternalUrl => $"/ATProto/ViewStandardSiteDocument?did={Author.DID}&rkey={RecordKey}";

        string? IPost.ExternalUrl => null;

        DateTimeOffset IPost.PostedAt => PublishedAt;

        string? IPost.ProfileUrl => Author.AvatarCID is string cid
            ? $"/ATProto/GetBlob?did={Author.DID}&cid={cid}"
            : null;

        IEnumerable<IPostThumbnail> IPost.Thumbnails => [];

        string? IPost.Username => Author.Handle ?? Author.DID;

        string? IPost.Usericon => Author.AvatarCID is string cid
            ? $"/ATProto/GetBlob?did={Author.DID}&cid={cid}"
            : null;
    }
}
