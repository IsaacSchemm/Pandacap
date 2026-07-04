using Microsoft.Extensions.Time.Testing;
using Pandacap.FurAffinity.Interfaces;
using System.Net;
using System.Net.Http.Headers;

namespace Pandacap.FurAffinity.Tests
{
    [TestClass]
    public sealed class FurAffinityOnlineStatsProviderTests
    {
        private class MockHttpMessageHandler(
            HttpMethod method,
            Uri uri,
            string htmlResponse
        ) : HttpMessageHandler, IFurAffinityHttpHandlerProvider
        {
            public int Calls { get; private set; }

            public HttpMessageHandler GetOrCreateHandler() => this;

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                Calls++;

                if (method != request.Method || uri != request.RequestUri)
                    throw new NotImplementedException($"No setup for {request.Method} {request.RequestUri}");

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        htmlResponse,
                        MediaTypeHeaderValue.Parse("text/html"))
                });
            }
        }

        [TestMethod]
        public async Task IsBotUsageOkAsync_UnderThreshold()
        {
            var httpMessageHandler = new MockHttpMessageHandler(
                HttpMethod.Get,
                new("https://www.furaffinity.net/help/"),
                @"<div>
                    <div class=""online-stats"">
                        84999 <strong><span title=""Measured in the last 900 seconds"">Users online</span></strong> &mdash;
                        30000 <strong>guests</strong>,
                        14999 <strong>registered</strong>
                        and 40000 <strong>other</strong>
                        <!-- Online Counter Last Update: Thu, 19 Mar 2026 18:27:00 -0700 -->
                    </div>
                </div>");

            var timeProvider = new FakeTimeProvider();

            IFurAffinityOnlineStatsProvider provider = new FurAffinityOnlineStatsProvider(
                httpMessageHandler,
                timeProvider);

            Assert.AreEqual(0, httpMessageHandler.Calls);

            Assert.IsTrue(await provider.IsBotUsageOkAsync());
            Assert.AreEqual(1, httpMessageHandler.Calls);

            timeProvider.Advance(TimeSpan.FromMinutes(4.5));

            Assert.IsTrue(await provider.IsBotUsageOkAsync());
            Assert.AreEqual(1, httpMessageHandler.Calls);

            timeProvider.Advance(TimeSpan.FromMinutes(1));

            Assert.IsTrue(await provider.IsBotUsageOkAsync());
            Assert.AreEqual(2, httpMessageHandler.Calls);
        }

        [TestMethod]
        public async Task IsBotUsageOkAsync_OverThreshold()
        {
            var httpMessageHandler = new MockHttpMessageHandler(
                HttpMethod.Get,
                new("https://www.furaffinity.net/help/"),
                @"<div>
                    <div class=""online-stats"">
                        85000 <strong><span title=""Measured in the last 900 seconds"">Users online</span></strong> &mdash;
                        30000 <strong>guests</strong>,
                        15000 <strong>registered</strong>
                        and 40000 <strong>other</strong>
                        <!-- Online Counter Last Update: Thu, 19 Mar 2026 18:27:00 -0700 -->
                    </div>
                </div>");

            var timeProvider = new FakeTimeProvider();

            IFurAffinityOnlineStatsProvider provider = new FurAffinityOnlineStatsProvider(
                httpMessageHandler,
                timeProvider);

            Assert.AreEqual(0, httpMessageHandler.Calls);

            Assert.IsFalse(await provider.IsBotUsageOkAsync());
            Assert.AreEqual(1, httpMessageHandler.Calls);

            timeProvider.Advance(TimeSpan.FromMinutes(54));

            Assert.IsFalse(await provider.IsBotUsageOkAsync());
            Assert.AreEqual(1, httpMessageHandler.Calls);

            timeProvider.Advance(TimeSpan.FromMinutes(52));

            Assert.IsFalse(await provider.IsBotUsageOkAsync());
            Assert.AreEqual(2, httpMessageHandler.Calls);
        }
    }
}
