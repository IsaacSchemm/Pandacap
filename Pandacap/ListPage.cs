using Microsoft.FSharp.Collections;
using Pandacap.UI.Elements;

namespace Pandacap
{
    public record ListPage<T>(FSharpList<T> Current, string? Next) where T : IPost;
}
