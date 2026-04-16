using Microsoft.FSharp.Collections;

namespace Pandacap.PlatformLinks.ProfileInformation.Interfaces
{
    public record PlatformLinkProfileInformation(
        FSharpList<string> BlueskyHandles,
        FSharpList<string> DeviantArtUsernames,
        FSharpList<string> FurAffinityUsernames,
        FSharpList<string> WeasylUsernames);
}
