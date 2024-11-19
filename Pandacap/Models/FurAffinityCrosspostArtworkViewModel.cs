using Microsoft.AspNetCore.Mvc.Rendering;

namespace Pandacap.Models
{
    public class FurAffinityCrosspostArtworkViewModel
    {
        public Guid Id { get; set; }

        public List<long> Folders { get; set; } = [];

        public List<SelectListItem> AvailableFolders { get; set; } = [];

        public bool Scraps { get; set; }

        public int Category { get; set; }

        public List<SelectListItem> AvailableCategories { get; set; } = [];

        public int Gender { get; set; }

        public List<SelectListItem> AvailableGenders { get; set; } = [];

        public int Species { get; set; }

        public List<SelectListItem> AvailableSpecies { get; set; } = [];

        public int Type { get; set; }

        public List<SelectListItem> AvailableTypes { get; set; } = [];
    }
}
