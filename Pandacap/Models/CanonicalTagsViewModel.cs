using Pandacap.CanonicalTags.Interfaces;

namespace Pandacap.Models
{
    public class CanonicalTagsViewModel
    {
        public IReadOnlyList<ICanonicalTagTreeDisplayNode> ArtMedia { get; set; } = [];
        public IReadOnlyList<ICanonicalTagTreeDisplayNode> Characters { get; set; } = [];
        public IReadOnlyList<ICanonicalTagTreeDisplayNode> Settings { get; set; } = [];
        public IReadOnlyList<ICanonicalTagTreeDisplayNode> Species { get; set; } = [];

        public IReadOnlyList<ICanonicalTagTreeDisplayNode> All => [
            .. ArtMedia.OrderBy(node => node.Name),
            .. Characters.OrderBy(node => node.Name),
            .. Settings.OrderBy(node => node.Name),
            .. Species.OrderBy(node => node.Name),
        ];
    }
}
