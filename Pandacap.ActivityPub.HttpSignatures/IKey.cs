namespace Pandacap.ActivityPub.HttpSignatures
{
    public interface IKey
    {
        string KeyId { get; }
        string KeyPem { get; }
    }
}
