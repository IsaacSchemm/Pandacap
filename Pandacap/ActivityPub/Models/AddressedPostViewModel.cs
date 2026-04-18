using Pandacap.ActivityPub.RemoteObjects.Models;
using Pandacap.ActivityPub.Replies.Interfaces;
using Pandacap.Database;

namespace Pandacap.Models
{
    public class AddressedPostViewModel
    {
        public required AddressedPost Post { get; set; }

        public required IEnumerable<RemoteAddressee> Users { get; set; }

        public required IEnumerable<RemoteAddressee> Communities { get; set; }

        public required IEnumerable<IReply> Replies { get; set; }
    }
}
