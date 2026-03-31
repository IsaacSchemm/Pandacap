using Pandacap.UI.Badges;
using Pandacap.UI.Elements;

namespace Pandacap.Database
{
    public class InboxWeasylJournal : IInboxPost
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = "";

        public string Username { get; set; } = "";

        public string? Avatar { get; set; }

        public DateTimeOffset PostedAt { get; set; }

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

        IEnumerable<IPostThumbnail> IPost.Thumbnails => [];

        string? IPost.Usericon => Avatar;
    }
}
