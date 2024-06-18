namespace Pandacap.Data
{
    public interface IUserPostImage
    {
        /// <summary>
        /// The URL to the attached image.
        /// </summary>
        string ImageUrl { get; set; }

        /// <summary>
        /// The expected media type of the image (such as image/png).
        /// </summary>
        string ImageContentType { get; set; }

        /// <summary>
        /// Alt text for the image, if any.
        /// </summary>
        string? AltText { get; set; }
    }
}
