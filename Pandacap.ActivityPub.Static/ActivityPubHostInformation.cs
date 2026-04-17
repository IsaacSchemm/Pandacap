using Pandacap.Configuration;

namespace Pandacap.ActivityPub.Static
{
    public static class ActivityPubHostInformation
    {
        public static string ApplicationHostname =>
            DeploymentInformation.ApplicationHostname;

        public static string Username =>
            DeploymentInformation.Username;

        public static string ActorId =>
            $"https://{ApplicationHostname}";

        public static string GenerateTransientObjectId() =>
            $"https://{ApplicationHostname}/ActivityPub/Transient/{Guid.NewGuid()}";
    }
}
