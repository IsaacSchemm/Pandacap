using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Pandacap.Models
{
    public abstract class CreatePostViewModel
    {
        [Required]
        [DisplayName("Body (Markdown)")]
        public string MarkdownBody { get; set; } = "";

        [DisplayName("Tags (space-separated, e.g. tag1 tag2 tag3)")]
        public string? Tags { get; set; }

        [DisplayName("Send this post to Bridgy Fed (if enabled)")]
        public bool BridgyFed { get; set; }

        public IEnumerable<string> DistinctTags =>
            (Tags ?? "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(tag => tag.TrimStart('#'))
            .Select(tag => tag.TrimEnd(','))
            .Distinct();
    }
}
