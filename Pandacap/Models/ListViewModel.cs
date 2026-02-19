using Pandacap.Data;

namespace Pandacap.Models
{
    public class ListViewModel
    {
        public required string? Title { get; set; }

        public string? Q { get; set; }

        public required IEnumerable<IPost> Items { get; set; }

        public string? Next { get; set; }

        public bool RSS { get; set; }

        public bool Atom { get; set; }
    }
}
