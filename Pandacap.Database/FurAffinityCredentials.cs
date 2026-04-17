using Pandacap.Constants;
using Pandacap.FurAffinity.Interfaces;
using Pandacap.UI.Badges;
using Pandacap.UI.Elements;
using System.ComponentModel.DataAnnotations;

namespace Pandacap.Database
{
    public class FurAffinityCredentials : IFurAffinityCredentials, IExternalCredentials
    {
        [Key]
        public string Username { get; set; } = "";

        public string A { get; set; } = "";
        public string B { get; set; } = "";

        string IFurAffinityCredentials.UserAgent =>
            UserAgentInformation.UserAgent;

        Badge IExternalCredentials.Badge => Badges.FurAffinity;

        string IExternalCredentials.PlatformName => "Fur Affinity";
    }
}
