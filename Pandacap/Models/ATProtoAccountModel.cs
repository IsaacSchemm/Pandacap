namespace Pandacap.Models
{
    public record ATProtoAccountModel
    {
        public required string PDS { get; init; }
        public required string DID { get; init; }
    }
}
