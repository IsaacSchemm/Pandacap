using Pandacap.CanonicalTags.Models;

namespace Pandacap.Models
{
    public class CanonicalTagsViewModel
    {
        public IReadOnlyList<CanonicalTagTreeDisplayNode> All { get; set; } = [];
    }
}
