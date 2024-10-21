using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Pandacap.Models
{
    public class CreateArtworkViewModel : CreatePostViewModel
    {
        [Required]
        public string Title { get; set; } = "";

        [Required]
        public IFormFile? File { get; set; }

        [DisplayName("Image description (alt text)")]
        public string? AltText { get; set; }
    }
}
