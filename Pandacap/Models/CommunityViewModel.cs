using Microsoft.FSharp.Collections;
using Pandacap.Lemmy.Models;

namespace Pandacap.Models
{
    public record CommunityViewModel(
        string ActorId,
        string Host,
        Community Community,
        int Page,
        FSharpList<PostView> PostObjects);
}
