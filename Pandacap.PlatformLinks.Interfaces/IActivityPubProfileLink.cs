namespace Pandacap.PlatformLinks.Interfaces
{
    public interface IActivityPubProfileLink
    {
        string? PlatformName { get; }
        string Username { get; }
        string? ViewProfileUrl { get; }
    }
}
