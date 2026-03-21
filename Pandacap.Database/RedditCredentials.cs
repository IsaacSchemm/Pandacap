using Pandacap.UI.Badges;
using Pandacap.UI.Elements;
using System.ComponentModel.DataAnnotations;

namespace Pandacap.Database
{
    public class RedditCredentials : IExternalCredentials
    {
        [Key]
        public string Username { get; set; } = "";

        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";

        Badge IExternalCredentials.Badge =>
            Badges.Reddit.WithText(Username);
    }
}
