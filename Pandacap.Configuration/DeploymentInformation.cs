namespace Pandacap.Configuration
{
    public static class DeploymentInformation
    {
        private static string? _hostname = null;
        private static string? _username = null;

        /// <summary>
        /// The host / domain name used by Pandacap.
        /// </summary>
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

        /// <summary>
        /// The displayed handle of the Pandacap instance owner.
        /// </summary>
        public static string Username
        {
            get
            {
                return _username ?? throw new Exception("Username not defined");
            }
            set
            {
                _username ??= value;
                if (_username != value)
                    throw new Exception("Username defined more than once with different values");
            }
        }
    }
}
