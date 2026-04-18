using Pandacap.Database;
using Pandacap.PostCreation.Interfaces;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Pandacap.Models
{
    public class CreateLinkFromUrlViewModel : INewPost
    {
        [Required]
        [DisplayName("URL")]
        [Url]
        [ReadOnly(true)]
        public string LinkUrl { get; set; } = "";

        [Required]
        public string Title { get; set; } = "";

        [DisplayName("Body (Markdown)")]
        public string? MarkdownBody { get; set; }

        [DisplayName("Tags (space-separated, e.g. tag1 tag2 tag3)")]
        public string? Tags { get; set; }

        Post.PostType INewPost.PostType => Post.PostType.Link;

        Guid? INewPost.UploadId => null;

        string? INewPost.AltText => null;

        bool INewPost.FocusTop => false;
    }
}
