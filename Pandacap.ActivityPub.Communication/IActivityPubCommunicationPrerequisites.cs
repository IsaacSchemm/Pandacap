namespace Pandacap.ActivityPub.Communication
{
    public interface IActivityPubCommunicationPrerequisites
    {
        string UserAgent { get; }

        Task<string> GetPublicKeyAsync();
        Task<byte[]> SignRsaSha256Async(byte[] data);
    }
}
