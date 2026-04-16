namespace Pandacap.PlatformLinks.Interfaces
{
    public interface IPlatformLinkProvider
    {
        IAsyncEnumerable<IPlatformLink> GetPostLinksAsync(
            IPlatformLinkPostSource post);

        IAsyncEnumerable<IPlatformLink> GetProfileLinksAsync();
    }
}
