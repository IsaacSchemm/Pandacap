namespace Pandacap.Weasyl.Tests
{
    [TestClass]
    public class WeasylHttpHandlerProviderTests
    {
        [TestMethod]
        public void GetOrCreateHandler_DoesNotAutoRedirect()
        {
            var provider = new WeasylHttpHandlerProvider();
            Assert.IsFalse(
                (provider.GetOrCreateHandler() as SocketsHttpHandler)?.AllowAutoRedirect);
        }

        [TestMethod]
        public void GetOrCreateHandler_LimitsPooledConnectionLifetime()
        {
            var provider = new WeasylHttpHandlerProvider();
            Assert.IsLessThanOrEqualTo(
                value: (provider.GetOrCreateHandler() as SocketsHttpHandler)!.PooledConnectionLifetime,
                upperBound: TimeSpan.FromMinutes(30));
        }

        [TestMethod]
        public void GetOrCreateHandler_ReusesHandler()
        {
            var provider = new WeasylHttpHandlerProvider();
            var handler1 = provider.GetOrCreateHandler();
            var handler2 = provider.GetOrCreateHandler();
            Assert.AreSame(handler1, handler2);
        }
    }
}
