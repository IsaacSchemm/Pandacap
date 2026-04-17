using Pandacap.PlatformLinks.Interfaces;

namespace Pandacap.Models
{
    public interface IProfileHeadingModel
    {
        IReadOnlyList<IPlatformLink> PlatformLinks { get; }
    }
}
