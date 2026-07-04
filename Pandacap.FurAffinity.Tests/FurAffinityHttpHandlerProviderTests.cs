namespace Pandacap.FurAffinity.Tests
{
    [TestClass]
    public class FurAffinityHttpHandlerProviderTests
    {
        [TestMethod]
        public void GetOrCreateHandler_DoesNotUseCookies()
        {
            IFurAffinityHttpHandlerProvider provider = new FurAffinityHttpHandlerProvider();
            Assert.IsFalse(
                ((SocketsHttpHandler)provider.GetOrCreateHandler()).UseCookies);
        }

        [TestMethod]
        public void GetOrCreateHandler_LimitsPooledConnectionLifetime()
        {
            IFurAffinityHttpHandlerProvider provider = new FurAffinityHttpHandlerProvider();
            Assert.IsLessThanOrEqualTo(
                value: ((SocketsHttpHandler)provider.GetOrCreateHandler()).PooledConnectionLifetime,
                upperBound: TimeSpan.FromMinutes(30));
        }

        [TestMethod]
        public void GetOrCreateHandler_ReusesHandler()
        {
            IFurAffinityHttpHandlerProvider provider = new FurAffinityHttpHandlerProvider();
            var handler1 = provider.GetOrCreateHandler();
            var handler2 = provider.GetOrCreateHandler();
            Assert.AreSame(handler1, handler2);
        }
    }
}
