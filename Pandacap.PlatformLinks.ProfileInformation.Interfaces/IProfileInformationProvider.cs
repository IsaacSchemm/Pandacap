namespace Pandacap.PlatformLinks.ProfileInformation.Interfaces
{
    public interface IProfileInformationProvider
    {
        Task<ProfileInformation> GetProfileInformationAsync(
            CancellationToken cancellationToken);
    }
}
