using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pandacap.Data
{
    public class DeviantArtInboxThumbnail
    {
        public string Url { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class DeviantArtInboxItem
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
        public List<DeviantArtInboxThumbnail> Thumbnails { get; set; } = [];
        public string? Excerpt { get; set; }
        public string? LinkUrl { get; set; }

        public DateTimeOffset? DismissedAt { get; set; }
    }
}
