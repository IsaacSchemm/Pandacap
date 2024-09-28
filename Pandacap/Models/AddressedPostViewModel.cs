using Pandacap.Data;
using Pandacap.JsonLd;

namespace Pandacap.Models
{
    public class AddressedPostViewModel
    {
        public required AddressedPost Post { get; set; }

        public required IEnumerable<RemoteAddressee> Users { get; set; }

        public required IEnumerable<RemoteAddressee> Communities { get; set; }

        public required IEnumerable<RemoteReplyModel> Replies { get; set; }
    }
}
