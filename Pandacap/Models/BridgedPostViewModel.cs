using Pandacap.Data;

namespace Pandacap.Models
{
    public class BridgedPostViewModel
    {
        public required UserPost Post { get; set; }

        public required IEnumerable<RemoteActivity> RemoteActivities { get; set; }
    }
}
