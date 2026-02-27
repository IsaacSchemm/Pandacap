using Pandacap.Data;
using Pandacap.HighLevel.PlatformLinks;

namespace Pandacap.Models
{
    public class UserPostViewModel : IProfileHeadingModel
    {
        public required IReadOnlyList<IPlatformLink> PlatformLinks { get; set; }

        public required Post Post { get; set; }

        public required IReadOnlyList<ReplyModel> Replies { get; set; }
    }
}
