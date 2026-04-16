using Microsoft.FSharp.Collections;

namespace Pandacap.PlatformLinks.ProfileInformation.Interfaces
{
    public record PlatformLinkProfileInformation(
        FSharpList<PlatformLinkATProtoAccount> BlueskyAccounts,
        FSharpList<string> DeviantArtUsernames,
        FSharpList<string> FurAffinityUsernames,
        FSharpList<string> WeasylUsernames);
}
