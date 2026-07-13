using Microsoft.AspNetCore.Mvc.Rendering;

namespace Pandacap.Models
{
    public class WeasylCrosspostArtworkViewModel
    {
        public Guid Id { get; set; }

        public int? FolderId { get; set; }

        public List<SelectListItem> AvailableFolders { get; set; } = [];
    }
}
