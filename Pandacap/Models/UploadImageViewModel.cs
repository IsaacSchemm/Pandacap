using System.ComponentModel;

namespace Pandacap.Models
{
    public class UploadImageViewModel : CreatePostViewModel
    {
        public required IFormFile File { get; set; }

        [DisplayName("Image description (alt text)")]
        public string? AltText { get; set; }

        [DisplayName("Send the image to Azure AI Vision to generate new alt text")]
        public bool GenerateAltText { get; set; }
    }
}
