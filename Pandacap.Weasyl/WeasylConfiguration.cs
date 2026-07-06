namespace Pandacap.Weasyl
{
    internal record WeasylConfiguration
    {
        /// <summary>
        /// A Weasyl API key.
        /// </summary>
        public required string WeasylApiKey { get; init; }
    }
}
