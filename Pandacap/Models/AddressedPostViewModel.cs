using Pandacap.Data;
using Pandacap.JsonLd;

namespace Pandacap.Models
{
    public class AddressedPostViewModel
    {
        public required AddressedPost Post { get; set; }
        public required IEnumerable<Addressee> Users { get; set; }
        public required IEnumerable<Addressee> Communities { get; set; }
    }
}
