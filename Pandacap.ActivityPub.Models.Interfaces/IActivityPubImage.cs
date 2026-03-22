namespace Pandacap.ActivityPub.Models.Interfaces
{
    public interface IActivityPubImage
    {
        string Url { get; }
        string AltText { get; }
        string MediaType { get; }
        string? HorizontalFocalPoint { get; }
        string? VerticalFocalPoint { get; }
    }
}
