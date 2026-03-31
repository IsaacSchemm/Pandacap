using Pandacap.UI.Badges;
using Pandacap.UI.Elements;

namespace Pandacap.Database
{
    public class FurAffinityFavorite : IFavorite, IPostThumbnail
    {
        public Guid Id { get; set; }

        public int SubmissionId { get; set; }

        public string Title { get; set; } = "";

        public string Thumbnail { get; set; } = "";

        public string Link { get; set; } = "";

        public class User
        {
            public string Name { get; set; } = "";
            public string ProfileName { get; set; } = "";
            public string Url { get; set; } = "";
            public string? AvatarUrl { get; set; }
        }

        public User PostedBy { get; set; } = new();

        public DateTimeOffset PostedAt { get; set; }

        public DateTimeOffset FavoritedAt { get; set; }

        public DateTimeOffset? HiddenAt { get; set; }

        Badge IPost.Badge => Badges.FurAffinity.WithHostFromUriString(Link);

        string IPost.DisplayTitle => Title;

        string IPost.Id => $"{Id}";

        string IPost.InternalUrl => Link;

        string IPost.ExternalUrl => Link;

        string? IPost.ProfileUrl => PostedBy.Url;

        IEnumerable<IPostThumbnail> IPost.Thumbnails => [this];

        string? IPost.Username => PostedBy.Name;

        string? IPost.Usericon => PostedBy.AvatarUrl;

        string IPostThumbnail.Url => Thumbnail;

        string IPostThumbnail.AltText => "";
    }
}
