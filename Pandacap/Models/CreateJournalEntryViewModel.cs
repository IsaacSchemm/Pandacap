using Pandacap.Data;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Pandacap.Models
{
    public class CreateJournalEntryViewModel : PostCreator.IViewModel
    {
        [Required]
        public string Title { get; set; } = "";

        [Required]
        [DisplayName("Body (Markdown)")]
        public string MarkdownBody { get; set; } = "";

        [DisplayName("Tags (space-separated, e.g. tag1 tag2 tag3)")]
        public string? Tags { get; set; }

        PostType PostCreator.IViewModel.PostType => PostType.JournalEntry;

        Guid? PostCreator.IViewModel.UploadId => null;

        string? PostCreator.IViewModel.AltText => null;

        bool PostCreator.IViewModel.FocusTop => false;
    }
}
