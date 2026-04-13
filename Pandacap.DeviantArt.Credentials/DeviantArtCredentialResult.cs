using DeviantArtFs;

namespace Pandacap.DeviantArt.Credentials
{
    internal record Result(
        IDeviantArtRefreshableAccessToken Token,
        DeviantArtFs.ResponseTypes.User User);
}
