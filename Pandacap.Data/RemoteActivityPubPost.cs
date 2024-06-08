namespace Pandacap.Data
{
    public class RemoteActivityPubPost : IPost
    {
        private static readonly Textify.HtmlToTextConverter _converter = new();

        public string Id { get; set; } = "";

        public string CreatedBy { get; set; } = "";

        public string? Username { get; set; }
        public string? Usericon { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string? Summary { get; set; }
        public bool Sensitive { get; set; }

        public string? Name { get; set; }
        public string? Content { get; set; }

        public class ActivityPubImageAttachment : IThumbnail, IThumbnailRendition
        {
            public string Url { get; set; } = "";
            public string? Name { get; set; }

            int IThumbnailRendition.Width => 0;
            int IThumbnailRendition.Height => 0;

            string? IThumbnail.AltText => Name;
            IEnumerable<IThumbnailRendition> IThumbnail.Renditions => [this];
        }

        public List<ActivityPubImageAttachment> Attachments { get; set; } = [];

        public bool? IsMention { get; set; }
        public bool? IsReply { get; set; }

        public DateTimeOffset? FavoritedAt { get; set; }
        public Guid? LikeGuid { get; set; }

        public DateTimeOffset? DismissedAt { get; set; }

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

                return Name ?? excerpt ?? $"{Id}";
            }
        }

        string? IPost.LinkUrl => Id;

        IEnumerable<IThumbnail> IPost.Thumbnails => Attachments;
    }
}
