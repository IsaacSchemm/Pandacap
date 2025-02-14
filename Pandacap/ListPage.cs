using Microsoft.FSharp.Core;
using Pandacap.Data;

namespace Pandacap
{
    public record ListPage(IReadOnlyList<IPost> Current, string? Next) : ActivityPub.IListPage
    {
        IEnumerable<object> ActivityPub.IListPage.Current =>
            Current;

        FSharpOption<string?> ActivityPub.IListPage.Next =>
            OptionModule.OfObj(Next);
    }
}
