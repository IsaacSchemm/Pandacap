using Pandacap.ActivityPub.Inbound;
using Pandacap.Data;

namespace Pandacap.Models
{
    public class AddressedPostViewModel
    {
        public required AddressedPost Post { get; set; }

        public required IEnumerable<RemoteAddressee> Users { get; set; }

        public required IEnumerable<RemoteAddressee> Communities { get; set; }

        public required IEnumerable<ReplyModel> Replies { get; set; }
    }
}
