using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Pandacap.LowLevel;

namespace Pandacap.HighLevel
{
    /// <summary>
    /// Provides access to an encryption key in Azure Key Vault. This key is
    /// used as the signing key for the ActivityPub actor.
    /// </summary>
    public class KeyProvider(ApplicationInformation appInfo)
    {
        private readonly Lazy<KeyClient> _keyClient = new(() => new KeyClient(
            new Uri($"https://{appInfo.KeyVaultHostname}"),
            new DefaultAzureCredential()));

        /// <summary>
        /// Retrieves the public key and renders it in PEM format for use in the ActivityPub actor object.
        /// </summary>
        /// <returns>An object that contains the public key in PEM format</returns>
        public async Task<ActorKey> GetPublicKeyAsync()
        {
            var key = await _keyClient.Value.GetKeyAsync("activitypub");
            byte[] arr = key.Value.Key.ToRSA().ExportSubjectPublicKeyInfo();
            string str = Convert.ToBase64String(arr);
            return new ActorKey($"-----BEGIN PUBLIC KEY-----\n{str}\n-----END PUBLIC KEY-----");
        }

        /// <summary>
        /// Creates a signature for the given data using the private key.
        /// </summary>
        /// <param name="data">The data to sign</param>
        /// <returns>An RSA SHA-256 signature</returns>
        public async Task<byte[]> SignRsaSha256Async(byte[] data)
        {
            var cryptographyClient = _keyClient.Value.GetCryptographyClient("activitypub");
            var result = await cryptographyClient.SignDataAsync(SignatureAlgorithm.RS256, data);
            return result.Signature;
        }
    }
}
