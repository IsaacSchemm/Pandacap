using Pandacap.Weasyl.Interfaces;

namespace Pandacap.Weasyl
{
    public class WeasylHttpHandlerProvider : IWeasylHttpHandlerProvider
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
