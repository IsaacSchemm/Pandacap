using Pandacap.Data;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Pandacap.Models
{
    public class CreateArtworkFromUploadViewModel : PostCreator.IViewModel
    {
        [Required]
        public string Title { get; set; } = "";

        [Required]
        [DisplayName("Body (Markdown)")]
        public string MarkdownBody { get; set; } = "";

        [DisplayName("Tags (space-separated, e.g. tag1 tag2 tag3)")]
        public string? Tags { get; set; }

        [Required]
        public Guid UploadId { get; set; }

        [DisplayName("Image description (alt text)")]
        public string? AltText { get; set; }

        [DisplayName("Crop image at top (instead of at center) in Mastodon")]
        public bool FocusTop { get; set; }

        PostType PostCreator.IViewModel.PostType => PostType.Artwork;

        Guid? PostCreator.IViewModel.UploadId => UploadId;
    }
}
