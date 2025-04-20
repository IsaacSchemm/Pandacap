using Microsoft.FSharp.Collections;
using Pandacap.Clients;

namespace Pandacap.Models
{
    public record CommunityViewModel(
        string ActorId,
        string Host,
        Lemmy.Community Community,
        int Page,
        FSharpList<Lemmy.PostView> PostObjects);
}
