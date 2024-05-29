using Pandacap.Data;

namespace Pandacap.Models
{
    public class ListViewModel
    {
        public string Controller { get; set; } = "";

        public string Action { get; set; } = "";

        public IEnumerable<IPost> Items { get; set; } = [];

        public int? PrevOffset { get; set; }

        public int? NextOffset { get; set; }

        public int Count { get; set; }
    }
}
