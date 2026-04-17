using Pandacap.ActivityPub.Models.Interfaces;
using Pandacap.Text;
using Pandacap.UI.Badges;
using Pandacap.UI.Elements;

namespace Pandacap.Database
{
    public class ActivityPubFavorite : IFavorite, IActivityPubLike
    {
        public Guid Id { get; set; }

        public string ObjectId { get; set; } = "";

        public string CreatedBy { get; set; } = "";

        public string? Username { get; set; }

        public string? Usericon { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset FavoritedAt { get; set; }

        public DateTimeOffset? HiddenAt { get; set; }

        public string? Summary { get; set; }

        public bool Sensitive { get; set; }

        public string? Name { get; set; }

        public string? Content { get; set; }

        public string? InReplyTo { get; set; }

        public class Image : IPostThumbnail
        {
            public string Url { get; set; } = "";

            public string? Name { get; set; }

            string IPostThumbnail.AltText => Name ?? "";
        }

        public List<Image> Attachments { get; set; } = [];

        Badge IPost.Badge => Badges.ActivityPub.WithHostFromUriString(ObjectId);

        string IPost.DisplayTitle => Name ?? TextConverter.FromHtml(Content);

        string IPost.Id => $"{Id}";

        string IPost.InternalUrl => $"/RemotePosts?id={Uri.EscapeDataString(ObjectId)}";

        string IPost.ExternalUrl => ObjectId;

        DateTimeOffset IPost.PostedAt => CreatedAt;

        string? IPost.ProfileUrl => CreatedBy;

        IEnumerable<IPostThumbnail> IPost.Thumbnails => Attachments;
    }
}
