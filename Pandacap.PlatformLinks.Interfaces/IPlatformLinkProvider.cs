namespace Pandacap.PlatformLinks.Interfaces
{
    public interface IPlatformLinkProvider
    {
        Task<IReadOnlyList<IPlatformLink>> GetPlatformLinksAsync(
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>> GetBlueskyStyleAppViewHostsAsync(
            CancellationToken cancellationToken);
    }
}
