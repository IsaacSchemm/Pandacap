using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Pandacap.Models
{
    public class CreateJournalEntryViewModel
    {
        [Required]
        public string Title { get; set; } = "";

        [DisplayName("Mark as sensitive or mature content")]
        public bool Sensitive { get; set; }

        [DisplayName("Hide post contents behind a summary")]
        public bool UseSummary { get; set; }

        public string? Summary { get; set; }

        [Required]
        [DisplayName("Body (Markdown)")]
        public string MarkdownBody { get; set; } = "";

        [DisplayName("Tags (comma-separated, e.g. tag1, tag2, tag3)")]
        public string? Tags { get; set; }
    }
}
