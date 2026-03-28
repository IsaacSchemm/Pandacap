namespace Pandacap.ActivityPub.HttpSignatures.Discovery.Models
{
    public record ActorKey : IKey
    {
        public required string KeyId { get; init; }
        public required string KeyPem { get; init; }
        public required string Owner { get; init; }
    }
}
