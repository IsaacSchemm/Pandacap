using Pandacap.Data;

namespace Pandacap.Models
{
    public class BlueskyCrosspostViewModel
    {
        public Post? Post { get; set; }

        public Guid Id { get; set; }

        public string TextContent { get; set; } = "";
    }
}
