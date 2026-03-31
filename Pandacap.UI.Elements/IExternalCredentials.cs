using Pandacap.UI.Badges;

namespace Pandacap.UI.Elements
{
    public interface IExternalCredentials
    {
        string Id { get; }
        string Username { get; }
        Badge Badge { get; }
    }
}
