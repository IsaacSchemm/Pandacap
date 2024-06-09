using System.ComponentModel.DataAnnotations;

namespace Pandacap.Data
{
    public class RemoteActivityPubAnnouncement : IPost
    {
        private static readonly Textify.HtmlToTextConverter _converter = new();

        [Key]
        public string AnnounceActivityId { get; set; } = "";

        public string ObjectId { get; set; } = "";

        public class User
        {
            public string Id { get; set; } = "";
            public string? Username { get; set; }
            public string? Usericon { get; set; }
        }

        public User CreatedBy { get; set; } = new();
        public User SharedBy { get; set; } = new();

        public DateTimeOffset SharedAt { get; set; }

        public string? Summary { get; set; }
        public bool Sensitive { get; set; }

        public string? Name { get; set; }
        public string? Content { get; set; }

        public class ImageAttachment : IThumbnail, IThumbnailRendition
        {
            public string Url { get; set; } = "";
            public string? Name { get; set; }

            int IThumbnailRendition.Width => 0;
            int IThumbnailRendition.Height => 0;

            string? IThumbnail.AltText => Name;
            IEnumerable<IThumbnailRendition> IThumbnail.Renditions => [this];
        }

        public List<ImageAttachment> Attachments { get; set; } = [];

        string? IPost.DisplayTitle
        {
            get
            {
                string? excerpt = Content == null
                    ? null
                    : _converter.Convert(Content)
                        .Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .FirstOrDefault();

                if (excerpt != null && excerpt.Length > 60)
                    excerpt = excerpt[..60] + "...";

                return Name ?? excerpt ?? $"{AnnounceActivityId}";
            }
        }

        string IPost.Id => AnnounceActivityId;

        string? IPost.Username => SharedBy.Username;

        string? IPost.Usericon => SharedBy.Usericon;

        DateTimeOffset IPost.Timestamp => SharedAt;

        string? IPost.LinkUrl => ObjectId;

        IEnumerable<IThumbnail> IPost.Thumbnails => Attachments;
    }
}
