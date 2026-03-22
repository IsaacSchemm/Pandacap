namespace Pandacap.ActivityPub.Models.Interfaces
{
    public interface IActivityPubLink
    {
        string Href { get; }
        string MediaType { get; }
    }
}
