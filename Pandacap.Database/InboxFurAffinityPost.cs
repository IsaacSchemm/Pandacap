using Pandacap.UI.Badges;
using Pandacap.UI.Elements;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pandacap.Database
{
    public abstract class InboxFurAffinityPost : IInboxPost
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = "";

        public class User
        {
            public string Name { get; set; } = "";
            public string Url { get; set; } = "";
            public string? Avatar { get; set; }
        }

        public User PostedBy { get; set; } = new();

        public DateTimeOffset PostedAt { get; set; }

        public DateTimeOffset? DismissedAt { get; set; }

        [NotMapped]
        public abstract string Url { get; }

        [NotMapped]
        public abstract IEnumerable<IPostThumbnail> Thumbnails { get; }

        bool IInboxPost.IsPodcast => false;

        bool IInboxPost.IsShare => false;

        Badge IPost.Badge => Badges.FurAffinity.WithHostFromUriString(Url);

        string IPost.DisplayTitle => Title;

        string IPost.Id => $"{Id}";

        string IPost.InternalUrl => Url;

        string IPost.ExternalUrl => Url;

        string? IPost.ProfileUrl => PostedBy.Url;

        IEnumerable<IPostThumbnail> IPost.Thumbnails => Thumbnails;

        string? IPost.Username => PostedBy.Name;

        string? IPost.Usericon => PostedBy.Avatar;
    }
}
