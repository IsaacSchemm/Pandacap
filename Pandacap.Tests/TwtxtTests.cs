using FluentAssertions;
using Microsoft.FSharp.Collections;
using Pandacap.LowLevel.Twtxt;
using System.Text;

namespace Pandacap.Tests
{
    [TestClass]
    public class TwtxtTests
    {
        [TestMethod]
        public void HashGenerator_Test()
        {
            var actual = HashGenerator.getHash(
                "http://0.0.0.0:8000/user/lizard-socks/twtxt.txt",
                new(
                    DateTimeOffset.Parse("2025-05-31T22:28:58Z"),
                    "hi",
                    ReplyContext.NoReplyContext));

            actual.Should().Be("rrjhxea");
        }

        [TestMethod]
        public void HashGenerator_Test_Multiline()
        {
            var actual = HashGenerator.getHash(
                "http://0.0.0.0:8000/user/lizard-socks/twtxt.txt",
                new(
                    DateTimeOffset.Parse("2025-05-31T22:06:56Z"),
                    "This is an example post\u2028Here is another line\u2028\u2028Here is another paragraph",
                    ReplyContext.NoReplyContext));

            actual.Should().Be("o6or74a");
        }

        [TestMethod]
        public void FeedBuilder_Test()
        {
            var feedData = FeedBuilder.BuildFeed(new(
                metadata: new(
                    url: [
                        new("https://www.example.com/first.txt"),
                        new("https://www.example.com/other.txt")
                    ],
                    nick: ["sample-name"],
                    avatar: ["https://www.example.com/icon.png"],
                    follow: [
                        new(url: new("https://www.example.com/he.txt"), text: "him"),
                        new(url: new("https://www.example.com/she.txt"),text:  "her")
                    ],
                    link: [
                        new(url: new("https://www.example.org/folder/file.png"), text: "A sample file"),
                        new(url: new("https://www.example.net"), text: "A website")
                    ],
                    refresh: [60 * 60 * 6],
                    prev: [
                        new("abcdefg", "a/b.c?d=e")]),
                twts: [
                    new(
                        new DateTimeOffset(1995, 12, 25, 23, 30, 0, 0, TimeSpan.FromHours(-6)),
                        "This is a local time sample",
                        ReplyContext.NewHash("abcdefg")),
                    new Twt(
                        new DateTimeOffset(2025, 4, 1, 8, 20, 55, 123, TimeSpan.Zero),
                        "This is a UTC sample",
                        ReplyContext.NoReplyContext)
                ]));

            string expectedStr = $@"# url = https://www.example.com/first.txt
# url = https://www.example.com/other.txt
# nick = sample-name
# avatar = https://www.example.com/icon.png
# follow = him https://www.example.com/he.txt
# follow = her https://www.example.com/she.txt
# link = A sample file https://www.example.org/folder/file.png
# link = A website https://www.example.net
# refresh = 21600
# prev = abcdefg a/b.c?d=e
1995-12-25T23:30:00-06:00{'\t'}(#abcdefg) This is a local time sample
2025-04-01T08:20:55Z{'\t'}This is a UTC sample";

            expectedStr = expectedStr.Replace("\r", "");

            Encoding.UTF8.GetString(feedData).Should().Be(expectedStr);
        }

        [TestMethod]
        public void FeedBuilder_Minimal()
        {
            var feedData = FeedBuilder.BuildFeed(new(
                metadata: new(
                    url: [],
                    nick: [],
                    avatar: [],
                    follow: [],
                    link: [],
                    refresh: [],
                    prev: []),
                twts: [
                    new(
                        new DateTimeOffset(1995, 12, 25, 23, 30, 0, 0, TimeSpan.FromHours(-6)),
                        "This is a local time sample",
                        ReplyContext.NewHash("abcdefg")),
                    new Twt(
                        new DateTimeOffset(2025, 4, 1, 8, 20, 55, 123, TimeSpan.Zero),
                        "This is a UTC sample",
                        ReplyContext.NoReplyContext)
                ]));

            string expectedStr = $@"1995-12-25T23:30:00-06:00{'\t'}(#abcdefg) This is a local time sample
2025-04-01T08:20:55Z{'\t'}This is a UTC sample";

            expectedStr = expectedStr.Replace("\r", "");

            Encoding.UTF8.GetString(feedData).Should().Be(expectedStr);
        }

        [TestMethod]
        public void FeedReader_Test()
        {
            var base64 = "IyBUd3R4dCBpcyBhbiBvcGVuLCBkaXN0cmlidXRlZCBtaWNyb2Jsb2dnaW5nIHBsYXRmb3JtIHRoYXQKIyB1c2VzIGh1bWFuLXJlYWRhYmxlIHRleHQgZmlsZXMsIGNvbW1vbiB0cmFuc3BvcnQgcHJvdG9jb2xzLAojIGFuZCBmcmVlIHNvZnR3YXJlLgojCiMgTGVhcm4gbW9yZSBhYm91dCB0d3R4dCBhdCAgaHR0cHM6Ly9naXRodWIuY29tL2J1Y2trZXQvdHd0eHQKIwojIFRoaXMgaXMgaG9zdGVkIGJ5IGEgWWFybi5zb2NpYWwgcG9kIHlhcm4ubG9jYWwgcnVubmluZyB5YXJuZCAwLjE1LjFAN2ZkM2RhZWQgMjAyMy0xMS0yNlQxMDo0MDoxMisxMDowMCBnbzEuMjEuNAojIExlYXJuIG1vcmUgYWJvdXQgWWFybi5zb2NpYWwgYXQgaHR0cHM6Ly95YXJuLnNvY2lhbAojCiMgbmljayAgICAgICAgPSBsaXphcmQtc29ja3MKIyB1cmwgICAgICAgICA9IGh0dHA6Ly8wLjAuMC4wOjgwMDAvdXNlci9saXphcmQtc29ja3MvdHd0eHQudHh0CiMgYXZhdGFyICAgICAgPSBodHRwOi8vMC4wLjAuMDo4MDAwL3VzZXIvbGl6YXJkLXNvY2tzL2F2YXRhcgojIGRlc2NyaXB0aW9uID0gCiMKIyBmb2xsb3dpbmcgICA9IDMKIyMKIyBmb2xsb3cgPSBsaXphcmQtc29ja3MgaHR0cDovLzAuMC4wLjA6ODAwMC91c2VyL2xpemFyZC1zb2Nrcy90d3R4dC50eHQKIyBmb2xsb3cgPSBuZXdzIGh0dHA6Ly8wLjAuMC4wOjgwMDAvdXNlci9uZXdzL3R3dHh0LnR4dAojIGZvbGxvdyA9IHN1cHBvcnQgaHR0cDovLzAuMC4wLjA6ODAwMC91c2VyL3N1cHBvcnQvdHd0eHQudHh0CgoyMDI1LTA1LTI5VDIzOjEyOjA1WglUaGlzIF9pc18gYW4gKipleGFtcGxlKiogcG9zdAoyMDI1LTA1LTI5VDIzOjEyOjIwWgkoI2JxM3ltM2EpIFRoaXMgaXMgYTrigKhSZXBseSHigKh0byB0aGF0IHBvc3QuCg==";

            var feed = FeedReader.ReadFeed(
                Convert.FromBase64String(
                    base64));

            var expected = new Feed(
                metadata: new(
                    url: [new("http://0.0.0.0:8000/user/lizard-socks/twtxt.txt")],
                    nick: ["lizard-socks"],
                    avatar: ["http://0.0.0.0:8000/user/lizard-socks/avatar"],
                    follow: [
                        new(text: "lizard-socks", url: new("http://0.0.0.0:8000/user/lizard-socks/twtxt.txt")),
                        new(text: "news", url: new("http://0.0.0.0:8000/user/news/twtxt.txt")),
                        new(text: "support", url: new("http://0.0.0.0:8000/user/support/twtxt.txt"))
                    ],
                    link: [],
                    refresh: [],
                    prev: []),
                twts: [
                    new(
                        DateTimeOffset.Parse("2025-05-29T23:12:05Z"),
                        "This _is_ an **example** post",
                        ReplyContext.NoReplyContext),
                    new(
                        DateTimeOffset.Parse("2025-05-29T23:12:20Z"),
                        "This is a:\nReply!\nto that post.",
                        ReplyContext.NewHash("bq3ym3a"))
                ]);

            feed.ToString().Should().Be(expected.ToString());
            feed.Should().Be(expected);
        }

        [TestMethod]
        public void ImageExtractor_Test()
        {
            FSharpList<Link> expected = [
                new("../simple.jpg", ""),
                new("with-title.jpg", ""),
                new("/with-alt-text.jpg", "Alt Text Here ")
            ];

            var actual = ImageExtractor.FromMarkdown(@"![](../simple.jpg)
L

P

![](with-title.jpg ""Title"")
![Alt Text Here ](/with-alt-text.jpg)");

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ImageExtractor_Test_Empty()
        {
            var actual = ImageExtractor.FromMarkdown(@"Sample text
abc

zyx");

            actual.IsEmpty.Should().BeTrue();
        }
    }
}
