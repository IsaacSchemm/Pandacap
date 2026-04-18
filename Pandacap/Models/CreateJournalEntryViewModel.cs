using Pandacap.Database;
using Pandacap.PostCreation.Interfaces;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Pandacap.Models
{
    public class CreateJournalEntryViewModel : INewPost
    {
        [Required]
        public string Title { get; set; } = "";

        [Required]
        [DisplayName("Body (Markdown)")]
        public string MarkdownBody { get; set; } = "";

        [DisplayName("Tags (space-separated, e.g. tag1 tag2 tag3)")]
        public string? Tags { get; set; }

        Post.PostType INewPost.PostType => Post.PostType.JournalEntry;

        Guid? INewPost.UploadId => null;

        string? INewPost.LinkUrl => null;

        string? INewPost.AltText => null;

        bool INewPost.FocusTop => false;
    }
}
