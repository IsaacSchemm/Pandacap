using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Pandacap.Models
{
    public class UploadViewModel
    {
        [Required]
        public IFormFile? File { get; set; }

        [DisplayName("Image description (alt text)")]
        public string? AltText { get; set; }

        [DisplayName("Send the image to Azure AI Vision to generate new alt text")]
        public bool GenerateAltText { get; set; }

        public UploadDestination Destination { get; set; }
    }
}
