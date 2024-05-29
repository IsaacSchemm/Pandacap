using Pandacap.Data;

namespace Pandacap.Models.Gallery
{
    public class ListViewModel
    {
        public IEnumerable<DeviantArtOurPost> Items { get; set; } = [];

        public int? PrevOffset { get; set; }

        public int? NextOffset { get; set; }

        public int Count { get; set; }
    }
}
