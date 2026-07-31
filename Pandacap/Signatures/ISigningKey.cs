using System.Security.Cryptography;

namespace Pandacap.Signatures
{
    public interface ISigningKey
    {
        Uri FediId { get; }

        RSA GetRsa();
    }
}
