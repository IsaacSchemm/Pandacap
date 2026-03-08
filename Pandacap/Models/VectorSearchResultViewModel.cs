using Pandacap.Data;

namespace Pandacap.Models
{
    public record VectorSearchResultViewModel(
        IPost Post,
        double? Score);
}
