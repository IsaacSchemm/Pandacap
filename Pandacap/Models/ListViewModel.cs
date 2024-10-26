using Pandacap.LowLevel;

namespace Pandacap.Models
{
    public class ListViewModel
    {
        public string? Title { get; set; }

        public bool GroupByUser { get; set; }

        public bool AllowDismiss { get; set; }

        public string? Q { get; set; }

        public required ListPage Items { get; set; }
    }
}
