namespace Pandacap.Models
{
    public class ListViewModel
    {
        public required string? Title { get; set; }

        public string? Q { get; set; }

        public required ListPage Items { get; set; }

        public bool RSS { get; set; }

        public bool Atom { get; set; }

        public bool Twtxt { get; set; }
    }
}
