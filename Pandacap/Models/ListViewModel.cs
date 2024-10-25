using Pandacap.LowLevel;
using Pandacap.Types;

namespace Pandacap.Models
{
    public class ListViewModel
    {
        public string? Title { get; set; }

        public ThumbnailMode ShowThumbnails { get; set; } = ThumbnailMode.Auto;

        public bool GroupByUser { get; set; }

        public bool CanBeSyndicationFeed { get; set; }

        public bool AllowDismiss { get; set; }

        public string? Q { get; set; }

        public required ListPage Items { get; set; }
    }
}
