using Pandacap.LowLevel;

namespace Pandacap.Models
{
    public class ListViewModel<T>
    {
        public string? Title { get; set; }

        public bool ShowThumbnails { get; set; }

        public bool GroupByUser { get; set; }

        public bool AllowDismiss { get; set; }

        public string? Q { get; set; }

        public ListPage<T> Items { get; set; } = ListPage.Empty<T>();
    }
}
