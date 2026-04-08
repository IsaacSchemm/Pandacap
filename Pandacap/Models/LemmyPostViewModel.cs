using Microsoft.FSharp.Collections;
using Pandacap.Lemmy.Models;

namespace Pandacap.Models
{
    public record LemmyPostViewModel(
        Community Community,
        PostView PostView,
        FSharpList<CommentBranch> Comments);
}
