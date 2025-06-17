using Pandacap.Clients.ATProto;

namespace Pandacap.Models
{
    public record BlueskyPostViewModel(
        Guid Id,
        Profile.ProfileResponse ProfileResponse,
        BlueskyFeed.Post Post);
}
