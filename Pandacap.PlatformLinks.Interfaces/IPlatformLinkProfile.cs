using Microsoft.FSharp.Collections;

namespace Pandacap.PlatformLinks.Interfaces
{
    public interface IPlatformLinkProfile
    {
        FSharpList<string> ActivityPubWebFingerHandles { get; }
        FSharpList<string> BlueskyHandles { get; }
        FSharpList<string> DeviantArtUsernames { get; }
        FSharpList<string> FurAffinityUsernames { get; }
        FSharpList<string> WeasylUsernames { get; }
    }
}
