namespace Pandacap.Data
{
    public abstract class RemoteActivityPubPost : IPost
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

        public DateTimeOffset? FavoritedAt { get; set; }
        public DateTimeOffset? DismissedAt { get; set; }

        public string? Content { get; set; }

        string? IPost.DisplayTitle
        {
            get
            {
                string? excerpt = Content == null
                    ? null
                    : _converter.Convert(Content)
                        .Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .FirstOrDefault();

                if (excerpt != null && excerpt.Length > 40)
                    excerpt = excerpt[..40] + "...";

                return Name ?? excerpt ?? $"{Id}";
            }
        }

        string? IPost.LinkUrl => Id;
    }
}
