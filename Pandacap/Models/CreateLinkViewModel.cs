using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Pandacap.Models
{
    public class CreateLinkViewModel
    {
        [Required]
        [DisplayName("URL")]
        [Url]
        public string LinkUrl { get; set; } = "";
    }
}
