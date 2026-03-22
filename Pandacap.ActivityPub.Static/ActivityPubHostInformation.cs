namespace Pandacap.ActivityPub.Static
{
    public static class ActivityPubHostInformation
    {
        private static string? _hostname = null;

        public static string ApplicationHostname
        {
            get
            {
                return _hostname ?? throw new Exception("ApplicationHostname not defined");
            }
            set
            {
                _hostname ??= value;
                if (_hostname != value)
                    throw new Exception("ApplicationHostname defined more than once with different values");
            }
        }

        public static string ActorId =>
            $"https://{ApplicationHostname}";

        public static string GenerateTransientObjectId() =>
            $"https://{ApplicationHostname}/ActivityPub/Transient/{Guid.NewGuid()}";
    }
}
