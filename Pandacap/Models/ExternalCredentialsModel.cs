using Pandacap.Data;

namespace Pandacap.Models
{
    public class ExternalCredentialsModel
    {
        public IReadOnlyList<IExternalCredentials> ExternalCredentials { get; set; } = [];
    }
}
