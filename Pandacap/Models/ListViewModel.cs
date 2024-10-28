using Pandacap.LowLevel;

namespace Pandacap.Models
{
    public class ListViewModel
    {
        public string? Title { get; set; }

        public string? Q { get; set; }

        public required ListPage Items { get; set; }
    }
}
