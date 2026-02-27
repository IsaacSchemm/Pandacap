using Pandacap.HighLevel.PlatformLinks;

namespace Pandacap.Models
{
    public interface IProfileHeadingModel
    {
        IReadOnlyList<IPlatformLink> PlatformLinks { get; }
    }
}
