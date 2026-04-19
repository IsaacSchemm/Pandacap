namespace Pandacap.Weasyl
{
    internal record WeasylConfiguration
    {
        /// <summary>
        /// A website that hosts PHP scripts which proxy requests to Weasyl (to avoid a filter on Azure's outgoing IP address blocks).
        /// </summary>
        public required Uri WeasylProxyHost { get; init; }
    }
}
