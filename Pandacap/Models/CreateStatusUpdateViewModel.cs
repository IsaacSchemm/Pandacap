using Pandacap.Database;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Pandacap.Models
{
    public class CreateStatusUpdateViewModel : PostCreator.IViewModel
    {
        [Required]
        [DisplayName("Body (Markdown)")]
        public string MarkdownBody { get; set; } = "";

        [DisplayName("Tags (space-separated, e.g. tag1 tag2 tag3)")]
        public string? Tags { get; set; }

        Post.PostType PostCreator.IViewModel.PostType => Post.PostType.StatusUpdate;

        string? PostCreator.IViewModel.Title => null;

        Guid? PostCreator.IViewModel.UploadId => null;

        string? PostCreator.IViewModel.AltText => null;

        string? PostCreator.IViewModel.LinkUrl => null;

        bool PostCreator.IViewModel.FocusTop => false;
    }
}
