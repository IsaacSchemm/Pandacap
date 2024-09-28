using Pandacap.Data;

namespace Pandacap.Models
{
    public class UserPostViewModel
    {
        public required UserPost Post { get; set; }

        public required IEnumerable<UserPostActivity> RemoteActivities { get; set; }

        public required IEnumerable<RemoteActivityPubReply> Replies { get; set; }
    }
}
