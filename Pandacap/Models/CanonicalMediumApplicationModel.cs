namespace Pandacap.Models
{
    public record CanonicalMediumApplicationModel
    {
        public required Guid MediumId { get; init; }
        public required string MediumName { get; init; }
    }
}
