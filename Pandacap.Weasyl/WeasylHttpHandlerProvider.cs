using Pandacap.Weasyl.Interfaces;

namespace Pandacap.Weasyl
{
    internal class WeasylHttpHandlerProvider
    {
        private readonly Lazy<SocketsHttpHandler> Handler = new(() => new()
        {
            AllowAutoRedirect = false,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5L)
        });

        public HttpMessageHandler GetOrCreateHandler() =>
            Handler.Value;
    }
}
