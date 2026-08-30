using Pandacap.UI.Elements;

namespace Pandacap.Models
{
    public class BlueskyProfileViewModel
    {
        public required string DID { get; set; }
        public required string Handle { get; set; }
        public required string? AvatarCID { get; set; }
        public required IReadOnlyList<IPost> ImagePosts { get; set; }
    }
}
