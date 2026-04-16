namespace Pandacap.PlatformLinks.ProfileInformation.Interfaces
{
    public interface IPlatformLinkProfileInformationProvider
    {
        Task<PlatformLinkProfileInformation> GetProfileInformationAsync(
            CancellationToken cancellationToken);
    }
}
