using Microsoft.FSharp.Collections;
using Pandacap.UI.Elements;

namespace Pandacap.UI.Lists
{
    /// <summary>
    /// A single page of a list. Contains the elements on the page, plus an opaque identifier for the first element of the next page (if any).
    /// </summary>
    /// <typeparam name="T">The type of item in the list</typeparam>
    /// <param name="Current">The items on this page</param>
    /// <param name="Next">The next item, if any</param>
    public record ListPage<T>(
        FSharpList<T> Current,
        string? Next) where T : IPost;
}
