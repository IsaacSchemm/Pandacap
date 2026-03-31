using Pandacap.UI.Badges;
using Pandacap.UI.Elements;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Pandacap.Database
{
    /// <summary>
    /// A post from a user who this instance's owner follows on DeviantArt.
    /// </summary>
    public abstract class InboxDeviation : IInboxPost
    {
        public Guid Id { get; set; }

        public Guid CreatedBy { get; set; }

        public string Username { get; set; } = "";

        public string? Usericon { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public bool MatureContent { get; set; }

        public string? Title { get; set; }

        public string LinkUrl { get; set; } = "";

        public DateTimeOffset? DismissedAt { get; set; }

        [NotMapped]
        public abstract IEnumerable<IPostThumbnail> Thumbnails { get; }

        bool IInboxPost.IsPodcast => false;

        bool IInboxPost.IsShare => false;

        Badge IPost.Badge => Badges.DeviantArt.WithHostFromUriString(LinkUrl);

        string IPost.DisplayTitle => Title ?? $"{Id}";

        string IPost.Id => $"{Id}";

        string IPost.InternalUrl => LinkUrl;

        string IPost.ExternalUrl => LinkUrl;

        DateTimeOffset IPost.PostedAt => Timestamp;

        string? IPost.ProfileUrl => $"https://www.deviantart.com/{Uri.EscapeDataString(Username)}";

        IEnumerable<IPostThumbnail> IPost.Thumbnails => Thumbnails;
    }
}
