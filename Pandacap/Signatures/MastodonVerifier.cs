// Adapted from Letterbook
// https://github.com/Letterbook/Letterbook/blob/b1616beaf49ddefea22de58f41783521e088ea10/Letterbook.Adapter.ActivityPub/Signatures/MastodonVerifier.cs
// GNU Affero General Public License v3.0

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using NSign;
using NSign.Signatures;
using Pandacap.HighLevel;

namespace Pandacap.Signatures;

public partial class MastodonVerifier
{
    private readonly HashSet<string> DerivedComponents = [
        Constants.DerivedComponents.Authority,
        Constants.DerivedComponents.Status,
        Constants.DerivedComponents.RequestTarget,
        Constants.DerivedComponents.TargetUri,
        Constants.DerivedComponents.Path,
        Constants.DerivedComponents.Method,
        Constants.DerivedComponents.Query,
        Constants.DerivedComponents.Scheme,
        Constants.DerivedComponents.QueryParam,
        Constants.DerivedComponents.SignatureParams
    ];

    [GeneratedRegex(@"\(.*\)")]
    private static partial Regex DerivedComponentsRegex();

    private static readonly char[] SpacesAndQuotes = [' ', '"'];
    private static readonly char[] SpacesAndParentheses = [' ', '(', ')'];

    private static readonly StringSplitOptions RemoveEmpty = StringSplitOptions.RemoveEmptyEntries;
    private static readonly StringSplitOptions Trim = StringSplitOptions.TrimEntries;

    public VerificationResult VerifyRequestSignature(HttpRequest message, RemoteActor remoteActor)
    {
        var builder = new MastodonComponentBuilder(message);
        var components = ParseMastodonSignatureComponents(message);
        var defaultResult = VerificationResult.NoMatchingVerifierFound;

        foreach (var parsed in components)
        {
            if (!Uri.TryCreate(parsed.keyId, UriKind.Absolute, out Uri? keyId))
                continue;
            if (!Uri.TryCreate(remoteActor.KeyId, UriKind.Absolute, out Uri? remoteKeyId))
                continue;
            if (keyId != remoteKeyId)
                continue;

            if (VerifySignature(parsed, remoteActor, builder))
                return VerificationResult.SuccessfullyVerified;

            defaultResult = VerificationResult.SignatureMismatch;
        }

        return defaultResult;
    }

    private IEnumerable<MastodonSignatureComponents> ParseMastodonSignatureComponents(HttpRequest message)
    {
        var mastodonSignatures = message.Headers[Constants.Headers.Signature]
            .Select(header => header?.Split(',', RemoveEmpty) ?? [])
            .Where(parts => parts.Length > 1);

        if (!mastodonSignatures.Any())
            yield break;

        foreach (string[] parts in mastodonSignatures)
            yield return ParseSignatureValue(parts);
    }

    private MastodonSignatureComponents ParseSignatureValue(IEnumerable<string> parts)
    {
        var components = new MastodonSignatureComponents();

        foreach (var (key, value) in parts.Select(part =>
        {
            var innerParts = part.Split('=', 2, Trim | RemoveEmpty);
            return (innerParts[0], innerParts[1]);
        }))
        {
            switch (key)
            {
                case "keyId":
                    components.keyId = value.Trim('"');
                    break;
                case "signature":
                    components.signature = value.Trim('"');
                    break;
                case "headers":
                    string headersString = value;

                    var spec = new SignatureInputSpec("spec");
                    var match = DerivedComponentsRegex().Match(headersString);
                    if (match.Success)
                    {
                        foreach (var token in match.Value.Split(SpacesAndParentheses, RemoveEmpty))
                        {
                            spec.SignatureParameters.AddComponent(new DerivedComponent("@" + token));
                        }
                    }

                    foreach (string s in headersString[(match!.Length + 1)..].Split(SpacesAndQuotes, RemoveEmpty | Trim))
                    {
                        spec.SignatureParameters.AddComponent(
                            DerivedComponents.Contains(s) ? new DerivedComponent(s)
                            : DerivedComponents.Contains("@" + s) ? new DerivedComponent("@" + s)
                            : new HttpHeaderComponent(s));
                    }

                    components.spec = spec;
                    break;
            }
        }

        return components;
    }

    private static bool VerifySignature(MastodonSignatureComponents components, RemoteActor remoteActor,
        MastodonComponentBuilder builder)
    {
        using var algorithm = RSA.Create();
        algorithm.ImportFromPem(remoteActor.KeyPem);
        builder.Visit(components.spec.SignatureParameters);
        return algorithm.VerifyData(
            Encoding.ASCII.GetBytes(builder.SigningDocument),
            Convert.FromBase64String(components.signature),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
    }

    private struct MastodonSignatureComponents
    {
        internal SignatureInputSpec spec;
        internal string keyId;
        internal string signature;
    }
}
