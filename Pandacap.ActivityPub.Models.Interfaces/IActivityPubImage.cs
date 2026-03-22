namespace Pandacap.ActivityPub.Models.Interfaces
{
    public interface IActivityPubImage
    {
        IActivityPubPandacapRelativePath Location { get; }
        string AltText { get; }
        string MediaType { get; }
        string? HorizontalFocalPoint { get; }
        string? VerticalFocalPoint { get; }
    }
}
