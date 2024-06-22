using Pandacap.Data;

namespace Pandacap.Models
{
    public class ActivityInfo {
        public required ActivityPubInboundActivity RemoteActivity { get; set; }
        public UserPost? Post { get; set; }
    }
}
