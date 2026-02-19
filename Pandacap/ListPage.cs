using Microsoft.FSharp.Collections;
using Pandacap.Data;

namespace Pandacap
{
    public record ListPage<T>(FSharpList<T> Current, string? Next) where T : IPost;
}
