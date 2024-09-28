using Pandacap.Data;

namespace Pandacap.Models
{
    public class RemoteReplyModel
    {
        public required RemoteActivityPubReply RemotePost { get; set; }

        public required IEnumerable<LocalReplyModel> LocalReplies { get; set; }
    }
}
