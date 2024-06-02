using Pandacap.Data;
using Pandacap.LowLevel;

namespace Pandacap.Models
{
    public class ListViewModel
    {
        public string Controller { get; set; } = "";

        public string Action { get; set; } = "";

        public ListPage<IPost> Items { get; set; } = ListPage.Empty<IPost>();
    }
}
