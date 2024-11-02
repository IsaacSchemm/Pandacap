using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Pandacap.Models
{
    public class CreateArtworkViewModel : CreatePostViewModel
    {
        [Required]
        public string Title { get; set; } = "";

        [Required]
        public Guid UploadId { get; set; }

        [DisplayName("Image description (alt text)")]
        public string? AltText { get; set; }

        [DisplayName("Crop image at top (instead of at center) in Mastodon")]
        public bool FocusTop { get; set; }
    }
}
