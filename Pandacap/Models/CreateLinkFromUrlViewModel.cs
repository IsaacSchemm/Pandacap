using Pandacap.Data;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Pandacap.Models
{
    public class CreateLinkFromUrlViewModel : PostCreator.IViewModel
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

        PostType PostCreator.IViewModel.PostType => PostType.Link;

        Guid? PostCreator.IViewModel.UploadId => null;

        string? PostCreator.IViewModel.AltText => null;

        bool PostCreator.IViewModel.FocusTop => false;
    }
}
