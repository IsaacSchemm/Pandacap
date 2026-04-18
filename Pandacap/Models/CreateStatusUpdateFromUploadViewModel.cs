using Pandacap.Database;
using Pandacap.PostCreation.Interfaces;
using System.ComponentModel;

namespace Pandacap.Models
{
    public class CreateStatusUpdateFromUploadViewModel : INewPost
    {
        [DisplayName("Body (Markdown)")]
        public string MarkdownBody { get; set; } = "";

        [DisplayName("Tags (space-separated, e.g. tag1 tag2 tag3)")]
        public string? Tags { get; set; }

        public required Guid UploadId { get; set; }

        [DisplayName("Image description (alt text)")]
        public string? AltText { get; set; }

        Post.PostType INewPost.PostType => Post.PostType.StatusUpdate;

        string? INewPost.Title => null;

        Guid? INewPost.UploadId => UploadId;

        string? INewPost.LinkUrl => null;

        bool INewPost.FocusTop => false;
    }
}
