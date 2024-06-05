using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pandacap.Data
{
    public class DeviantArtCredentials
    {
        [Key]
        public string UserId { get; set; } = "";

        [ForeignKey(nameof(UserId))]
        public IdentityUser? User { get; set; }

        [Required]
        public string AccessToken { get; set; } = "";

        [Required]
        public string RefreshToken { get; set; } = "";
    }
}
