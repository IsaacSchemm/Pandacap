namespace Pandacap.Models
{
    public class VectorSearchViewModel
    {
        public required string Q { get; set; }

        public required IEnumerable<VectorSearchResultViewModel> Items { get; set; }

        public required int Skip { get; set; }
    }
}
