using Pandacap.UI.Elements;

namespace Pandacap.Models
{
    public class ExternalCredentialsModel
    {
        public IReadOnlyList<IExternalCredentials> ExternalCredentials { get; set; } = [];
    }
}
