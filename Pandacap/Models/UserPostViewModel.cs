using Pandacap.Data;
using Pandacap.HighLevel.PlatformLinks;

namespace Pandacap.Models
{
    public class UserPostViewModel
    {
        public required IEnumerable<IPlatformLink> PlatformLinks { get; set; }

        public required Post Post { get; set; }

        public required IEnumerable<ReplyModel> Replies { get; set; }
    }
}
