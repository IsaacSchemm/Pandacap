using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pandacap.Data
{
    public abstract class DeviantArtInboxPost : IInboxPost
    {
        public Guid Id { get; set; }

        [Required]
        public string UserId { get; set; } = "";

        [ForeignKey(nameof(UserId))]
        public IdentityUser? User { get; set; }

        public Guid CreatedBy { get; set; }

        public string? Username { get; set; }
        public string? Usericon { get; set; }

        public DateTimeOffset Timestamp { get; set; }
        public bool MatureContent { get; set; }

        public string? Title { get; set; }
        public string? LinkUrl { get; set; }

        public DateTimeOffset? DismissedAt { get; set; }

        string IInboxPost.Id => $"{Id}";

        string? IInboxPost.DisplayTitle
        {
            get
            {
                string? excerpt = (this as DeviantArtInboxTextPost)?.Excerpt;
                if (excerpt != null && excerpt.Length > 40)
                    excerpt = excerpt[..40] + "...";

                return Title ?? excerpt ?? $"{Id}";
            }
        }
    }
}
