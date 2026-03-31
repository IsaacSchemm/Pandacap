using Pandacap.UI.Elements;

namespace Pandacap.Models
{
    public record VectorSearchResultViewModel(
        IPost Post,
        double? Score);
}
