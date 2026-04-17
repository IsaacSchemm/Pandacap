using Pandacap.UI.Badges;
using Pandacap.UI.Elements;

namespace Pandacap.Database
{
    public class WeasylFavoriteSubmission : IFavorite
    {
        public Guid Id { get; set; }

        public int Submitid { get; set; }

        public string Title { get; set; } = "";

        public class User
        {
            public string Login { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string? Avatar { get; set; }
        }

        public User PostedBy { get; set; } = new();

        public DateTimeOffset PostedAt { get; set; }

        public class Image : IPostThumbnail
        {
            public string Url { get; set; } = "";

            string IPostThumbnail.AltText => "";
        }

        public List<Image> Thumbnails { get; set; } = [];

        public string ProfileUrl { get; set; } = "";

        public string Url { get; set; } = "";

        public DateTimeOffset FavoritedAt { get; set; }

        public DateTimeOffset? HiddenAt { get; set; }

        public Badge Badge => Badges.Weasyl.WithHostFromUriString(Url);

        public string DisplayTitle => Title;

        string IPost.Id => $"{Id}";

        public string InternalUrl => Url;

        public string ExternalUrl => Url;

        IEnumerable<IPostThumbnail> IPost.Thumbnails => Thumbnails;

        public string? Username => PostedBy.DisplayName;

        public string? Usericon => PostedBy.Avatar;
    }
}
