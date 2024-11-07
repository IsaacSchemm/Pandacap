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

        [DisplayName("Send the image to Azure to generate alt text using AI image analysis")]
        public bool GenerateDescription { get; set; }

        [DisplayName("Send the image to Azure to generate alt text using OCR")]
        public bool PerformOCR { get; set; }

        public UploadDestination Destination { get; set; }
    }
}
