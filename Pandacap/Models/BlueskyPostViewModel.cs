using Pandacap.Clients.ATProto.Public;

namespace Pandacap.Models
{
    public record BlueskyPostViewModel(
        Guid Id,
        Profile.ProfileResponse ProfileResponse,
        BlueskyFeed.Post Post);
}
