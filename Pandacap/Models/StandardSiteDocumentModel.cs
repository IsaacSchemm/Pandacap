using Pandacap.ATProto.Models;

namespace Pandacap.Models
{
    public class StandardSiteDocumentModel
    {
        public required string DID { get; init; }
        public required StandardSitePublication? Publication { get; init; }
        public required StandardSiteDocument Document { get; init; }
    }
}
