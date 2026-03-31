using Pandacap.UI.Badges;

namespace Pandacap.UI.Elements
{
    public interface IExternalCredentials
    {
        string Username { get; }
        string PlatformName { get; }
        Badge Badge { get; }
    }
}
