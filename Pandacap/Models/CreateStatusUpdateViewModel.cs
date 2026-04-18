using Pandacap.Database;
using Pandacap.PostCreation.Interfaces;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Pandacap.Models
{
    public class CreateStatusUpdateViewModel : INewPost
    {
        [Required]
        [DisplayName("Body (Markdown)")]
        public string MarkdownBody { get; set; } = "";

        [DisplayName("Tags (space-separated, e.g. tag1 tag2 tag3)")]
        public string? Tags { get; set; }

        Post.PostType INewPost.PostType => Post.PostType.StatusUpdate;

        string? INewPost.Title => null;

        Guid? INewPost.UploadId => null;

        string? INewPost.AltText => null;

        string? INewPost.LinkUrl => null;

        bool INewPost.FocusTop => false;
    }
}
