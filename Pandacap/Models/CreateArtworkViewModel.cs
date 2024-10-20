using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Pandacap.Models
{
    public class CreateArtworkViewModel
    {
        [Required]
        public string Title { get; set; } = "";

        [Required]
        public IFormFile? File { get; set; }

        [DisplayName("Image description (alt text)")]
        public string? AltText { get; set; }

        [Required]
        [DisplayName("Body (Markdown)")]
        public string MarkdownBody { get; set; } = "";

        [DisplayName("Tags (comma-separated, e.g. tag1, tag2, tag3)")]
        public string? Tags { get; set; }
    }
}
