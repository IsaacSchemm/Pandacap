using Microsoft.FSharp.Core;
using Pandacap.Data;

namespace Pandacap
{
    public record ListPage(IReadOnlyList<IPost> Current, string? Next);
}
