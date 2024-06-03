using Pandacap.Data;
using Pandacap.LowLevel;

namespace Pandacap.Models
{
    public class ListViewModel
    {
        public string? Title { get; set; }

        public string Controller { get; set; } = "";

        public string Action { get; set; } = "";

        public string? Q { get; set; }

        public ListPage<IPost> Items { get; set; } = ListPage.Empty<IPost>();
    }
}
