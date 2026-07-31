using Letterbook.Adapter.ActivityPub.Signatures;
using Letterbook.Api.Authentication.HttpSignature.Verification;
using Pandacap.ActivityPub.HttpSignatures.Discovery.Models;
using Pandacap.ActivityPub.HttpSignatures.Validation.Interfaces;
using Pandacap.ActivityPub.HttpSignatures.Validation.Models;
using System.Security.Cryptography;

namespace Pandacap.Signatures
{
    public class RFC9421SignatureValidator(ILoggerFactory loggerFactory) : IActivityPubSignatureValidator
    {
        public async Task<VerificationResult> VerifyRequestSignatureAsync(HttpRequest message, IKey key, CancellationToken cancellationToken)
        {
            var verifier = new FederatedActorHttpSignatureVerifier(
                loggerFactory,
                new VerificationKeyProvider(key));

            return await verifier.VerifyAsync(message.HttpContext, cancellationToken).AnyAsync(cancellationToken)
                ? VerificationResult.SuccessfullyVerified
                : VerificationResult.NoMatchingVerifierFound;
        }

        private class VerificationKeyProvider(IKey key) : IVerificationKeyProvider, ISigningKey
        {
            Uri ISigningKey.FediId => new(key.KeyId);

            async Task<ISigningKey?> IVerificationKeyProvider.GetKeyByIdAsync(string keyId, CancellationToken _)
            {
                if (keyId == key.KeyId)
                    return this;

                return null;
            }

            RSA ISigningKey.GetRsa()
            {
                var algorithm = RSA.Create();
                algorithm.ImportFromPem(key.KeyPem);
                return algorithm;
            }
        }
    }
}
