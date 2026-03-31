using Pandacap.UI.Badges;
using Pandacap.UI.Elements;

namespace Pandacap.Database
{
    public class DeviantArtFavorite : IFavorite
    {
        public Guid Id { get; set; }

        public Guid CreatedBy { get; set; }

        public string Username { get; set; } = "";

        public string? Usericon { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string? Title { get; set; }

        public string? Content { get; set; }

        public string LinkUrl { get; set; } = "";

        public List<string> ThumbnailUrls { get; set; } = [];

        public DateTimeOffset FavoritedAt { get; set; }

        public DateTimeOffset? HiddenAt { get; set; }

        Badge IPost.Badge => Badges.DeviantArt.WithHostFromUriString(LinkUrl);

        string IPost.DisplayTitle => Title ?? "";

        string IPost.Id => $"{Id}";

        string IPost.InternalUrl => LinkUrl;

        string IPost.ExternalUrl => LinkUrl;

        DateTimeOffset IPost.PostedAt => Timestamp;

        string? IPost.ProfileUrl => $"https://www.deviantart.com/{Uri.EscapeDataString(Username)}";

        IEnumerable<IPostThumbnail> IPost.Thumbnails => ThumbnailUrls.Select(url => new PostThumbnail(url));

        private record PostThumbnail(string Url) : IPostThumbnail
        {
            public string AltText => "";
        }
    }
}
