using DeviantArtFs;

namespace Pandacap.Credentials
{
    internal record Result(
        IDeviantArtRefreshableAccessToken Token,
        DeviantArtFs.ResponseTypes.User User);
}
