namespace Pandacap.ActivityPub.HttpSignatures.Discovery.Models
{
    public interface IKey
    {
        string KeyId { get; }
        string KeyPem { get; }
    }
}
