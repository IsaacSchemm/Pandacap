using Microsoft.FSharp.Collections;

namespace Pandacap
{
    public record AllowedExternalUserCollection(
        FSharpSet<string> DeviantArtUsers);
}
