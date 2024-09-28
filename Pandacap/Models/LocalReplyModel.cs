using Pandacap.Data;

namespace Pandacap.Models
{
    public class LocalReplyModel
    {
        public required AddressedPost LocalPost { get; set; }

        public required IEnumerable<RemoteReplyModel> RemoteReplies { get; set; }
    }
}
