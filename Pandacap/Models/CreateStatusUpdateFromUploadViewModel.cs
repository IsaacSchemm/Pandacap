using Pandacap.Data;
using System.ComponentModel;

namespace Pandacap.Models
{
    public class CreateStatusUpdateFromUploadViewModel : PostCreator.IViewModel
    {
        [DisplayName("Body (Markdown)")]
        public string MarkdownBody { get; set; } = "";

        [DisplayName("Tags (space-separated, e.g. tag1 tag2 tag3)")]
        public string? Tags { get; set; }

        public required Guid UploadId { get; set; }

        [DisplayName("Image description (alt text)")]
        public string? AltText { get; set; }

        PostType PostCreator.IViewModel.PostType => PostType.StatusUpdate;

        string? PostCreator.IViewModel.Title => null;

        Guid? PostCreator.IViewModel.UploadId => UploadId;

        bool PostCreator.IViewModel.FocusTop => false;
    }
}
