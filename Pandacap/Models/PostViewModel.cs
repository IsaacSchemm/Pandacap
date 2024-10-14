using Microsoft.FSharp.Collections;
using Pandacap.LowLevel;

namespace Pandacap.Models
{
    public record PostViewModel(
        Lemmy.Community Community,
        Lemmy.PostView PostView,
        FSharpList<Lemmy.CommentBranch> Comments);
}
