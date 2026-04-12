namespace Pandacap.KeyVault
{
    public record KeyVaultConfiguration
    {
        /// <summary>
        /// The URI of the key vault used for the signing key for ActivityPub.
        /// </summary>
        public required Uri KeyVaultHost { get; init; }
    }
}
