using Microsoft.FSharp.Collections;
using Pandacap.UI.Elements;

namespace Pandacap.Models
{
    public record BlueskyProfileViewModel(
        string DID,
        string Handle,
        string? AvatarCID,
        FSharpList<IPost> Posts);
}
