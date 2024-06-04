using Pandacap.LowLevel;

namespace Pandacap.Models
{
    public class ListViewModel<T>
    {
        public string? Title { get; set; }

        public string Controller { get; set; } = "";

        public string Action { get; set; } = "";

        public string? Q { get; set; }

        public ListPage<T> Items { get; set; } = ListPage.Empty<T>();
    }
}
