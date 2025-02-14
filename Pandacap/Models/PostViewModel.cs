using Microsoft.FSharp.Collections;
using Pandacap.Clients;

namespace Pandacap.Models
{
    public record PostViewModel(
        Lemmy.Community Community,
        Lemmy.PostView PostView,
        FSharpList<Lemmy.CommentBranch> Comments);
}
