using Letterbook.Adapter.ActivityPub.Signatures;
using NSign;
using NSign.Http;
using NSign.Providers;
using Pandacap.Signatures;
using System.Runtime.CompilerServices;

namespace Letterbook.Api.Authentication.HttpSignature.Verification;

public class FederatedActorHttpSignatureVerifier(
	ILoggerFactory loggerFactory,
	IVerificationKeyProvider verificationKeyProvider) : IFederatedActorHttpSignatureVerifier
{
	private static readonly HttpFieldOptions HttpFieldOptions = new();

	private readonly ILogger _logger = loggerFactory.CreateLogger<FederatedActorHttpSignatureVerifier>();

	public async IAsyncEnumerable<Uri> VerifyAsync(
		HttpContext context,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		var signingContext = new AspNetCoreMessageSigningContext(_logger, HttpFieldOptions, context);
		if (signingContext.HasSignaturesForVerification)
		{
			await foreach (var specVerified in VerifyRfcSignature(signingContext, cancellationToken))
			{
				yield return specVerified;
			}
		}
		else
		{
			await foreach (var mastodonVerified in VerifyMastodonSignature(context.Request, cancellationToken))
			{
				yield return mastodonVerified;
			}
		}
	}

	private async IAsyncEnumerable<Uri> VerifyMastodonSignature(HttpRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		yield break;
	}

	private async IAsyncEnumerable<Uri> VerifyRfcSignature(AspNetCoreMessageSigningContext signingContext, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		foreach (var signatureContext in signingContext.SignaturesForVerification)
		{
			var signatureParams = signatureContext.SignatureParams;
			var key = await GetKey(signatureParams.KeyId, cancellationToken);
			if (key is null)
			{
				continue;
			}

			var verifier = GetVerifier(signatureParams.Algorithm, signatureParams.KeyId, key);
			var input = signingContext.GetSignatureInput(signatureParams, out _);
			var verificationResult = await verifier.VerifyAsync(
				signatureParams,
				input,
				signatureContext.Signature,
				cancellationToken);

			if (verificationResult is VerificationResult.SuccessfullyVerified)
			{
				_logger.LogInformation("HTTP request signature validation succeeded");
				yield return key.FediId;
			}
		}
	}

	private async Task<ISigningKey?> GetKey(string? keyId, CancellationToken cancellationToken)
	{
		var key = keyId == null ? default : await verificationKeyProvider.GetKeyByIdAsync(keyId, cancellationToken);
		if (key == null)
		{
			_logger.LogWarning($"Unable to verify signature: key with ID {keyId} was not found");
		}
		return key;
	}

	private IVerifier GetVerifier(string? algorithm, string? keyId, ISigningKey? key)
	{
		if (key == null)
		{
			throw new SignatureVerificationException($"No public key for {keyId}");
		}

		return new RsaPssSha512SignatureProvider(null, key.GetRsa(), keyId);
	}
}