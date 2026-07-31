using Pandacap.Signatures;

namespace Letterbook.Adapter.ActivityPub.Signatures;

public interface IVerificationKeyProvider
{
	Task<ISigningKey?> GetKeyByIdAsync(string keyId, CancellationToken cancellationToken = default);
}