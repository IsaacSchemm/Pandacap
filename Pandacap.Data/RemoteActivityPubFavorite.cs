using System.ComponentModel.DataAnnotations;

namespace Pandacap.Data
{
    public class RemoteActivityPubFavorite : IPost
    {
        private static readonly Textify.HtmlToTextConverter _converter = new();

        [Key]
        public Guid LikeGuid { get; set; }

        [Required]
        public string ObjectId { get; set; } = "";

        [Required]
        public string CreatedBy { get; set; } = "";

        public string? Username { get; set; }
        public string? Usericon { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset FavoritedAt { get; set; }

        public string? Summary { get; set; }
        public bool Sensitive { get; set; }

        public string? Name { get; set; }
        public string? Content { get; set; }

        public class ImageAttachment : IThumbnail, IThumbnailRendition
        {
            [Required]
            public string Url { get; set; } = "";

            public string? Name { get; set; }

            int IThumbnailRendition.Width => 0;
            int IThumbnailRendition.Height => 0;

            string? IThumbnail.AltText => Name;
            IEnumerable<IThumbnailRendition> IThumbnail.Renditions => [this];
        }

        public List<ImageAttachment> Attachments { get; set; } = [];

        string IPost.Id => $"{LikeGuid}";

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

                return Name ?? excerpt ?? $"{LikeGuid}";
            }
        }

        string? IPost.LinkUrl => ObjectId;

        DateTimeOffset IPost.Timestamp => FavoritedAt;

        IEnumerable<IThumbnail> IPost.Thumbnails => Attachments;
    }
}
