using Pandacap.UI.Badges;
using Pandacap.UI.Elements;
using System.ComponentModel.DataAnnotations;

namespace Pandacap.Database
{
    public class DeviantArtCredentials : IExternalCredentials
    {
        [Key]
        public string Username { get; set; } = "";

        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";

        Badge IExternalCredentials.Badge =>
            Badges.DeviantArt.WithText(Username);
    }
}
