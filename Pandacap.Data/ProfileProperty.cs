using System.ComponentModel.DataAnnotations;

namespace Pandacap.Data
{
    public class ProfileProperty
    {
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

        [Required]
        public string Value { get; set; } = "";

        public string? Link { get; set; }
    }
}
