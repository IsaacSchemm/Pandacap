using Pandacap.UI.Badges;
using Pandacap.UI.Elements;

namespace Pandacap.Database
{
    public class InboxWeasylSubmission : IInboxPost
    {
        public Guid Id { get; set; }

        public int Submitid { get; set; }

        public string Title { get; set; } = "";

        public string Rating { get; set; } = "";

        public class User
        {
            public string Login { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string Avatar { get; set; } = "";
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

        public DateTimeOffset? DismissedAt { get; set; }

        bool IInboxPost.IsPodcast => false;

        bool IInboxPost.IsShare => false;

        Badge IPost.Badge => Badges.Weasyl.WithHostFromUriString(Url);

        string IPost.DisplayTitle => Title;

        string IPost.Id => $"{Id}";

        string IPost.InternalUrl => Url;

        string IPost.ExternalUrl => Url;

        IEnumerable<IPostThumbnail> IPost.Thumbnails => Thumbnails;

        string? IPost.Username => PostedBy.DisplayName;

        string? IPost.Usericon => PostedBy.Avatar;
    }
}
