using System.ComponentModel;

namespace Pandacap.Models
{
    public class CreateArtworkViewModel
    {
        public string Title { get; set; } = "";

        [DisplayName("Mark as sensitive or mature content")]
        public bool Sensitive { get; set; }

        [DisplayName("Hide post contents behind a summary")]
        public bool UseSummary { get; set; }

        public string Summary { get; set; } = "";

        public IFormFile? File { get; set; }

        [DisplayName("Image description (alt text)")]
        public string AltText { get; set; } = "";

        [DisplayName("Body (Markdown)")]
        public string MarkdownBody { get; set; } = "";

        [DisplayName("Tags (comma-separated, e.g. tag1, tag2, tag3)")]
        public string Tags { get; set; } = "";
    }
}
