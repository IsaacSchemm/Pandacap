using Pandacap.Data;
using System.ComponentModel;

namespace Pandacap.Models
{
    public class CreateStatusUpdateFromUploadViewModel : ICreatePostViewModel
    {
        [DisplayName("Body (Markdown)")]
        public string MarkdownBody { get; set; } = "";

        [DisplayName("Tags (space-separated, e.g. tag1 tag2 tag3)")]
        public string? Tags { get; set; }

        public required Guid UploadId { get; set; }

        [DisplayName("Image description (alt text)")]
        public string? AltText { get; set; }

        PostType ICreatePostViewModel.PostType => PostType.StatusUpdate;

        string? ICreatePostViewModel.Title => null;

        Guid? ICreatePostViewModel.UploadId => UploadId;

        bool ICreatePostViewModel.FocusTop => false;
    }
}
