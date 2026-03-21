using Pandacap.UI.Badges;
using Pandacap.UI.Elements;
using System.ComponentModel.DataAnnotations;

namespace Pandacap.Database
{
    public class WeasylCredentials : IExternalCredentials
    {
        [Key]
        public string Login { get; set; } = "";

        public string ApiKey { get; set; } = "";

        Badge IExternalCredentials.Badge =>
            Badges.Weasyl.WithText(Login);
    }
}
