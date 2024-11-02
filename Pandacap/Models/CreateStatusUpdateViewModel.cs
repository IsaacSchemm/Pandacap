using System.ComponentModel;

namespace Pandacap.Models
{
    public class CreateStatusUpdateViewModel : CreatePostViewModel
    {
        public Guid? PhotoBinImageId { get; set; }

        [DisplayName("Image description (alt text)")]
        public string? AltText { get; set; }
    }
}
