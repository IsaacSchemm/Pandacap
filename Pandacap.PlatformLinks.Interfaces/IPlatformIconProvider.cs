namespace Pandacap.PlatformLinks.Interfaces
{
    public interface IPlatformIconProvider
    {
        Task<string?> ResolveIconAsync(
            string host,
            CancellationToken cancellationToken = default);
    }
}
