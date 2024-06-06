using Pandacap.Data;

namespace Pandacap.Models
{
    public class BridgedPostViewModel
    {
        public required IUserDeviation Deviation { get; set; }

        public required IEnumerable<RemoteActivity> RemoteActivities { get; set; }
    }
}
