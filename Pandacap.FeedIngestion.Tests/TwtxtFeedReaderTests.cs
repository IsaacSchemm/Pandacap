using Moq;
using Pandacap.FeedIngestion.Interfaces;
using System.Net;
using System.Text;

namespace Pandacap.FeedIngestion.Tests
{
    [TestClass]
    public sealed class TwtxtFeedReaderTests
    {
        [TestMethod]
        public async Task ReadFeedAsync_ReadsYarnFeed()
        {
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            var httpClient = new HttpClient();

            var feedRequestHandlerMock = new Mock<IFeedRequestHandler>(MockBehavior.Strict);
            feedRequestHandlerMock
                .Setup(handler => handler.GetAsync(
                    "https://example.net/user/user123/twtxt.txt",
                    cancellationToken))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(YARN_FEED, Encoding.UTF8, "text/plain")
                });

            IFeedReader reader = new TwtxtFeedReader(
                feedRequestHandlerMock.Object);

            var feed = await reader
                .ReadFeedAsync(
                    "https://example.net/user/user123/twtxt.txt",
                    "text/plain")
                .ToListAsync(cancellationToken);

            Assert.HasCount(2, feed);

            Assert.AreEqual("user123", feed[0].FeedTitle);
            Assert.AreEqual("https://example.net/user/user123/twtxt.txt", feed[0].FeedWebsiteUrl);
            Assert.AreEqual("https://example.net/user/user123/avatar#lgwkahvc4w37v3rf35q7brb7kwgvezejtlpzcwlxrmavf6o64uea", feed[0].FeedIconUrl);
            Assert.IsNull(feed[0].Title);
            Assert.IsNotNull(feed[0].HtmlBody);
            Assert.AreEqual("Test 1234 🎶", feed[0].TextBody);
            Assert.AreEqual("https://example.net/user/user123/twtxt.txt", feed[0].Url);
            Assert.AreEqual(DateTimeOffset.Parse("2025-04-06T12:05:30Z"), feed[0].Timestamp);
            Assert.IsNull(feed[0].ThumbnailUrl);
            Assert.IsNull(feed[0].ThumbnailAltText);
            Assert.IsNull(feed[0].AudioUrl);
            Assert.IsNull(feed[0].DismissedAt);
        }

        private const string YARN_FEED = @"# Twtxt is an open, distributed microblogging platform that
# uses human-readable text files, common transport protocols,
# and free software.
#
# Learn more about twtxt at  https://github.com/buckket/twtxt
#
# This is hosted by a Yarn.social pod example.net running yarnd 0.15.1@556917e8 2025-11-22T09:59:17+10:00 go1.25.4
# Learn more about Yarn.social at https://yarn.social
#
# nick        = user123
# url         = https://example.net/user/user123/twtxt.txt
# avatar      = https://example.net/user/user123/avatar#lgwkahvc4w37v3rf35q7brb7kwgvezejtlpzcwlxrmavf6o64uea
# description = Description Here
#
# following   = 31
#
# link = Name Here https://www.example.com/
#
# follow = abc@bridge.example.net https://bridge.example.net/abc
# follow = def@feeds.example.net https://feeds.example.net/def



2025-04-06T12:05:30Z	Test 1234 🎶
2025-04-06T06:19:43Z	(#abcdefg) Test 5678";
    }
}
