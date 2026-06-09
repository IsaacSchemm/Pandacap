using Pandacap.CanonicalTags.Tree.Models;

namespace Pandacap.Models
{
    public class EditCanonicalTagsViewModel
    {
        public IReadOnlyList<CanonicalTagTreeDisplayNode> All { get; set; } = [];
    }
}
