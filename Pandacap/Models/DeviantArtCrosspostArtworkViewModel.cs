using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel;

namespace Pandacap.Models
{
    public class DeviantArtCrosspostArtworkViewModel
    {
        public Guid Id { get; set; }

        [DisplayName("Gallery folders")]
        public List<Guid> GalleryFolders { get; set; } = [];

        public List<SelectListItem> AvailableGalleryFolders { get; set; } = [];

        [DisplayName("AI-generated")]
        public bool IsAiGenerated { get; set; }

        [DisplayName("Not authorized for inclusion in third-party AI datasets")]
        public bool NoAi { get; set; }
    }
}
