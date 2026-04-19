namespace Pandacap.FurAffinity.Tests
{
    [TestClass]
    public class FurAffinityHttpHandlerProviderTests
    {
        [TestMethod]
        public void GetOrCreateHandler_DoesNotUseCookies()
        {
            var provider = new FurAffinityHttpHandlerProvider();
            Assert.IsFalse(
                provider.GetOrCreateHandler().UseCookies);
        }

        [TestMethod]
        public void GetOrCreateHandler_LimitsPooledConnectionLifetime()
        {
            var provider = new FurAffinityHttpHandlerProvider();
            Assert.IsLessThanOrEqualTo(
                value: provider.GetOrCreateHandler().PooledConnectionLifetime,
                upperBound: TimeSpan.FromMinutes(30));
        }

        [TestMethod]
        public void GetOrCreateHandler_ReusesHandler()
        {
            var provider = new FurAffinityHttpHandlerProvider();
            var handler1 = provider.GetOrCreateHandler();
            var handler2 = provider.GetOrCreateHandler();
            Assert.AreSame(handler1, handler2);
        }
    }
}
