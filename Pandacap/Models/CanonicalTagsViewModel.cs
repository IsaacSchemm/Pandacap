using Pandacap.CanonicalTags.Interfaces;

namespace Pandacap.Models
{
    public class CanonicalTagsViewModel
    {
        public IReadOnlyList<ICanonicalTagTreeDisplayNode> All { get; set; } = [];
    }
}
