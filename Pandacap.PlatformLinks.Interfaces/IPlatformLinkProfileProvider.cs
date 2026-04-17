namespace Pandacap.PlatformLinks.Interfaces
{
    public interface IPlatformLinkProfileProvider
    {
        Task<IPlatformLinkProfile> GetProfileInformationAsync(
            CancellationToken cancellationToken);
    }
}
