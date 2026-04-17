using Pandacap.Text;
using Pandacap.UI.Badges;
using Pandacap.UI.Elements;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pandacap.Database
{
    public class InboxActivityStreamsPost : IInboxPost
    {
        public Guid Id { get; set; }

        public string? AnnounceId { get; set; }

        public string ObjectId { get; set; } = "";

        public class User
        {
            public string Id { get; set; } = "";

            public string? Username { get; set; }

            public string? Usericon { get; set; }
        }

        public User Author { get; set; } = new();

        public User PostedBy { get; set; } = new();

        public DateTimeOffset PostedAt { get; set; }

        public string? Summary { get; set; }

        public bool Sensitive { get; set; }

        public string? Name { get; set; }

        public string? Content { get; set; }

        public class Image : IPostThumbnail
        {
            public string Url { get; set; } = "";

            public string? Name { get; set; }

            string IPostThumbnail.AltText => Name ?? "";
        }

        public List<Image> Attachments { get; set; } = [];

        public DateTimeOffset? DismissedAt { get; set; }

        [NotMapped]
        public string TextContent => TextConverter.FromHtml(Content);

        bool IInboxPost.IsPodcast => false;

        bool IInboxPost.IsShare => PostedBy.Id != Author.Id;

        Badge IPost.Badge => Badges.ActivityPub.WithHostFromUriString(ObjectId);

        string IPost.DisplayTitle => Summary ?? Name ?? ExcerptGenerator.FromText(60, TextContent);

        string IPost.Id => $"{Id}";

        string IPost.InternalUrl => $"/RemotePosts?id={Uri.EscapeDataString(ObjectId)}";

        string IPost.ExternalUrl => ObjectId;

        string? IPost.ProfileUrl => PostedBy.Id;

        IEnumerable<IPostThumbnail> IPost.Thumbnails => Attachments;

        string? IPost.Username => PostedBy.Username;

        string? IPost.Usericon => PostedBy.Usericon;
    }
}
