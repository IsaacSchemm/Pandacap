namespace Pandacap.PlatformLinks.Interfaces
{
    public interface IPlatformLinkProvider
    {
        IAsyncEnumerable<IPlatformLink> GetPostLinksAsync(
            IPlatformLinkPost post);

        IAsyncEnumerable<IPlatformLink> GetProfileLinksAsync();
    }
}
