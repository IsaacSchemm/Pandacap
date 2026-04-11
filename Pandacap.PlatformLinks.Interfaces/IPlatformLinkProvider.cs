namespace Pandacap.PlatformLinks.Interfaces
{
    public interface IPlatformLinkProvider
    {
        Task<IReadOnlyList<IPlatformLink>> GetPostLinksAsync(
            IPlatformLinkPostSource post,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<IPlatformLink>> GetProfileLinksAsync(
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>> GetBlueskyStyleAppViewHostsAsync(
            CancellationToken cancellationToken);
    }
}
