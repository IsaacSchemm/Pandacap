using Pandacap.FurAffinity.Interfaces;
using Pandacap.FurAffinity.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;

namespace Pandacap.FurAffinity.Tests
{
    [TestClass]
    public sealed class FurAffinityClientTests
    {
        private record MockCredentials(
            string A,
            string B,
            string UserAgent) : IFurAffinityCredentials;

        private static IFurAffinityCredentials GenerateCredentials() => new MockCredentials(
            $"{Guid.NewGuid()}",
            $"{Guid.NewGuid()}",
            $"{Guid.NewGuid()}");

        private record MockHttpTransaction(
            HttpMethod Method,
            Uri RequestUri,
            HttpResponseMessage ResponseMessage,
            IReadOnlyList<Action<HttpRequestMessage>> RequestMessageExpectations);

        private class MockHttpMessageHandler(
            params MockHttpTransaction[] mockHttpTransactions) : HttpMessageHandler
        {
            public bool Received { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                foreach (var transaction in mockHttpTransactions)
                {
                    if (transaction.Method != request.Method) continue;
                    if (transaction.RequestUri != request.RequestUri) continue;

                    foreach (var expectation in transaction.RequestMessageExpectations)
                        expectation(request);

                    Received = true;

                    transaction.ResponseMessage.RequestMessage = request;

                    return Task.FromResult(transaction.ResponseMessage);
                }


                throw new NotImplementedException($"No setup for {request.Method} {request.RequestUri}");
            }
        }

        private static string GetHtmlFromResource(string fileName)
        {
            var assembly = Assembly.GetAssembly(typeof(FurAffinityClientTests));
            using var stream = assembly!.GetManifestResourceStream($"{assembly.GetName().Name}.{fileName}");
            using var sr = new StreamReader(stream!);
            return sr.ReadToEnd();
        }

        private static IReadOnlyList<Action<HttpRequestMessage>> GetHeaderChecks(IFurAffinityCredentials credentials) => [
            req => Assert.AreEqual(
                actual: req.Headers.GetValues("Cookie").Single(),
                expected: $"a={credentials.A}; b={credentials.B}"),
            req => Assert.AreEqual(
                actual: req.Headers.UserAgent.ToString(),
                expected: credentials.UserAgent)
        ];

        [TestMethod]
        [DataRow("helpPageModern.html")]
        public async Task WhoamiAsync_FindsUsername(string fileName)
        {
            var credentials = GenerateCredentials();

            var httpMessageHandler = new MockHttpMessageHandler(
                new MockHttpTransaction(
                    HttpMethod.Get,
                    new("https://www.furaffinity.net/help/"),
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            GetHtmlFromResource(fileName),
                            MediaTypeHeaderValue.Parse("text/html"))
                    },
                    GetHeaderChecks(credentials)));

            IFurAffinityClient client = new FurAffinityClient(
                httpMessageHandler,
                Domain.WWW,
                credentials);

            var result = await client.WhoamiAsync(CancellationToken.None);

            Assert.AreEqual(
                actual: result,
                expected: "usernameHere");
        }

        [TestMethod]
        [DataRow("browsePageModern.html")]
        public async Task ListPostOptionsAsync_CollectsAllOptions(string fileName)
        {
            var credentials = GenerateCredentials();

            var httpMessageHandler = new MockHttpMessageHandler(
                new MockHttpTransaction(
                    HttpMethod.Get,
                    new("https://www.furaffinity.net/browse/"),
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            GetHtmlFromResource(fileName),
                            MediaTypeHeaderValue.Parse("text/html"))
                    },
                    GetHeaderChecks(credentials)));

            IFurAffinityClient client = new FurAffinityClient(
                httpMessageHandler,
                Domain.WWW,
                credentials);

            var result = await client.ListPostOptionsAsync(CancellationToken.None);

            Assert.AreEqual(
                actual: result.Categories.First(),
                expected: new PostOption("All", "1"));

            Assert.AreEqual(
                actual: result.Categories.Last(),
                expected: new PostOption("Other", "31"));

            Assert.AreEqual(
                actual: result.Types.First(),
                expected: new PostOption("All", "1"));

            Assert.AreEqual(
                actual: result.Types.Last(),
                expected: new PostOption("Other Music", "200"));

            Assert.AreEqual(
                actual: result.Species.First(),
                expected: new PostOption("Unspecified / Any", "1"));

            Assert.AreEqual(
                actual: result.Species.Last(),
                expected: new PostOption("Zebra", "6071"));

            Assert.AreEqual(
                actual: result.Genders.First(),
                expected: new PostOption("Any", ""));

            Assert.AreEqual(
                actual: result.Genders.Last(),
                expected: new PostOption("Non-Binary", "non_binary"));
        }

        [TestMethod]
        [DataRow("foldersPageModern.html")]
        public async Task ListGalleryFoldersAsync_CollectsAllOptions(string fileName)
        {
            var credentials = GenerateCredentials();

            var httpMessageHandler = new MockHttpMessageHandler(
                new MockHttpTransaction(
                    HttpMethod.Get,
                    new("https://www.furaffinity.net/controls/folders/submissions/"),
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            GetHtmlFromResource(fileName),
                            MediaTypeHeaderValue.Parse("text/html"))
                    },
                    GetHeaderChecks(credentials)));

            IFurAffinityClient client = new FurAffinityClient(
                httpMessageHandler,
                Domain.WWW,
                credentials);

            var result = await client.ListGalleryFoldersAsync(CancellationToken.None);

            Assert.AreEqual(
                actual: result,
                expected: [
                    new GalleryFolder(
                        folderId: 1286055,
                        name: "Folder1 (Folder)"),
                    new GalleryFolder(
                        folderId: 1286059,
                        name: "FolderX (Folder)")
                ]);
        }

        [TestMethod]
        public void PostArtworkAsync() =>
            Assert.Inconclusive("No unit tests implemented for write operations");

        [TestMethod]
        [DataRow("favoritesPageModern.html")]
        public async Task GetFavoritesAsync_CollectsSubmissions_FirstPage(string fileName)
        {
            var credentials = GenerateCredentials();

            var httpMessageHandler = new MockHttpMessageHandler(
                new MockHttpTransaction(
                    HttpMethod.Get,
                    new("https://www.furaffinity.net/favorites/fender"),
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            GetHtmlFromResource(fileName),
                            MediaTypeHeaderValue.Parse("text/html"))
                    },
                    GetHeaderChecks(credentials)));

            IFurAffinityClient client = new FurAffinityClient(
                httpMessageHandler,
                Domain.WWW,
                credentials);

            var result = await client.GetFavoritesAsync(
                "fender",
                FavoritesPage.First,
                CancellationToken.None);

            Assert.AreEqual(46, result.Length);

            Assert.AreEqual(
                actual: result.First(),
                expected: new Submission(
                    id: 1868857,
                    fav_id: 23675986,
                    submission_data: new(
                        avatar_mtime: "1749375654",
                        description: "A wallpaper I made for everyone to enjoy!\r\nI hope you like it, it\u0026#039;s my gift to all furs ^^",
                        lower: "electrocat",
                        title: "Furaffinity Wallpaper",
                        username: "EC Tiger"),
                    title: "Furaffinity Wallpaper",
                    thumbnail: "//t.furaffinity.net/1868857@300-1231398587.jpg"));

            Assert.AreEqual(
                actual: result.Last(),
                expected: new Submission(
                    id: 676625,
                    fav_id: 11386397,
                    submission_data: new(
                        avatar_mtime: "1435837741",
                        description: "So [i]thaaaaats[/i] where I\u0026#039;ve been. ;]\r\n\r\nDone for the cover of FA:United\u0026#039;s conbook! ( http://www.faunited.org )\r\nFA:U, FurAffinity\u0026#039;s very own convention, is only a few weeks away! You should come!\r\n\r\nSlightly larger version is on my livejournal:\r\nhttp://screwbald.livejournal.com/35813.html",
                        lower: "blotch",
                        title: "FA: United Cover",
                        username: "Blotch"),
                    title: "FA: United Cover",
                    thumbnail: "//t.furaffinity.net/676625@400-1185106060.jpg"));
        }

        [TestMethod]
        [DataRow("favoritesPageModern.html")]
        public async Task GetFavoritesAsync_CollectsSubmissions_NextPage(string fileName)
        {
            var credentials = GenerateCredentials();

            var httpMessageHandler = new MockHttpMessageHandler(
                new MockHttpTransaction(
                    HttpMethod.Get,
                    new("https://www.furaffinity.net/favorites/fender/30844931/next"),
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            GetHtmlFromResource(fileName),
                            MediaTypeHeaderValue.Parse("text/html"))
                    },
                    GetHeaderChecks(credentials)));

            IFurAffinityClient client = new FurAffinityClient(
                httpMessageHandler,
                Domain.WWW,
                credentials);

            var result = await client.GetFavoritesAsync(
                "fender",
                FavoritesPage.NewAfter(30844931),
                CancellationToken.None);

            Assert.AreEqual(46, result.Length);

            Assert.AreEqual(
                actual: result.First(),
                expected: new Submission(
                    id: 1868857,
                    fav_id: 23675986,
                    submission_data: new(
                        avatar_mtime: "1749375654",
                        description: "A wallpaper I made for everyone to enjoy!\r\nI hope you like it, it\u0026#039;s my gift to all furs ^^",
                        lower: "electrocat",
                        title: "Furaffinity Wallpaper",
                        username: "EC Tiger"),
                    title: "Furaffinity Wallpaper",
                    thumbnail: "//t.furaffinity.net/1868857@300-1231398587.jpg"));

            Assert.AreEqual(
                actual: result.Last(),
                expected: new Submission(
                    id: 676625,
                    fav_id: 11386397,
                    submission_data: new(
                        avatar_mtime: "1435837741",
                        description: "So [i]thaaaaats[/i] where I\u0026#039;ve been. ;]\r\n\r\nDone for the cover of FA:United\u0026#039;s conbook! ( http://www.faunited.org )\r\nFA:U, FurAffinity\u0026#039;s very own convention, is only a few weeks away! You should come!\r\n\r\nSlightly larger version is on my livejournal:\r\nhttp://screwbald.livejournal.com/35813.html",
                        lower: "blotch",
                        title: "FA: United Cover",
                        username: "Blotch"),
                    title: "FA: United Cover",
                    thumbnail: "//t.furaffinity.net/676625@400-1185106060.jpg"));
        }

        [TestMethod]
        [DataRow("submissionsPageModern.html")]
        public async Task GetSubmissionsAsync_CollectsSubmissions(string fileName)
        {
            var credentials = GenerateCredentials();

            var httpMessageHandler = new MockHttpMessageHandler(
                new MockHttpTransaction(
                    HttpMethod.Get,
                    new("https://sfw.furaffinity.net/msg/submissions/old~64114904@48/"),
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            GetHtmlFromResource(fileName),
                            MediaTypeHeaderValue.Parse("text/html"))
                    },
                    GetHeaderChecks(credentials)));

            IFurAffinityClient client = new FurAffinityClient(
                httpMessageHandler,
                Domain.SFW,
                credentials);

            var result = await client.GetSubmissionsAsync(
                SubmissionsPage.NewFromOldest(64114904),
                CancellationToken.None);

            Assert.AreEqual(
                actual: result,
                expected: [
                    new Submission(
                        id: 123456,
                        fav_id: 0,
                        submission_data: new(
                            avatar_mtime: "metadata am1",
                            description: "metadata b1",
                            lower: "metadata l1",
                            title: "metadata t1",
                            username: "metadata u1"),
                        title: "title Here",
                        thumbnail: "//t.furaffinity.net/123456@300-1771893106.jpg"),
                    new Submission(
                        id: 654321,
                        fav_id: 0,
                        submission_data: new(
                            avatar_mtime: "metadata am2",
                            description: "metadata b2",
                            lower: "metadata l2",
                            title: "metadata t2",
                            username: "metadata u2"),
                        title: "title There",
                        thumbnail: "//t.furaffinity.net/654321@200-1772268786.jpg")
                    ]);
        }

        [TestMethod]
        [DataRow("notesPageModern.html")]
        public async Task GetNotesAsync_CollectsNotes(string fileName)
        {
            var credentials = GenerateCredentials();

            var httpMessageHandler = new MockHttpMessageHandler(
                new MockHttpTransaction(
                    HttpMethod.Get,
                    new("https://www.furaffinity.net/msg/pms"),
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            GetHtmlFromResource(fileName),
                            MediaTypeHeaderValue.Parse("text/html"))
                    },
                    GetHeaderChecks(credentials)));

            IFurAffinityClient client = new FurAffinityClient(
                httpMessageHandler,
                Domain.WWW,
                credentials);

            var result = await client.GetNotesAsync(CancellationToken.None);

            Assert.AreEqual(
                actual: result,
                expected: [
                    new Note(
                        147099089,
                        subject: " demo ",
                        userDisplayName: "usernameDisplay1",
                        time: new DateTimeOffset(2025, 11, 10, 0, 25, 35, TimeSpan.Zero)),
                    new Note(
                        110906505,
                        subject: "SUBJECT HERE",
                        userDisplayName: "Username2",
                        time: new DateTimeOffset(2019, 12, 26, 3, 41, 55, TimeSpan.Zero)),
                    new Note(
                        109173624,
                        subject: " RE: Hey there ",
                        userDisplayName: null,
                        time: new DateTimeOffset(2019, 10, 7, 5, 15, 54, TimeSpan.Zero)),
                    ]);
        }

        [TestMethod]
        [DataRow("journalPageModern.html")]
        public async Task GetJournalAsync_GetsJournal(string fileName)
        {
            var credentials = GenerateCredentials();

            var httpMessageHandler = new MockHttpMessageHandler(
                new MockHttpTransaction(
                    HttpMethod.Get,
                    new("https://www.furaffinity.net/journal/11321713/"),
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            GetHtmlFromResource(fileName),
                            MediaTypeHeaderValue.Parse("text/html"))
                    },
                    GetHeaderChecks(credentials)));

            IFurAffinityClient client = new FurAffinityClient(
                httpMessageHandler,
                Domain.WWW,
                credentials);

            var result = await client.GetJournalAsync(
                11321713,
                CancellationToken.None);

            Assert.AreEqual(
                actual: result,
                expected: new Journal(
                    "Testing Journal Crossposting -- lizard-socks' Journal",
                    "https://www.furaffinity.net/journal/11321713/",
                    "https://a.furaffinity.net/1638829864/lizard-socks.gif"));
        }

        [TestMethod]
        [DataRow("notificationsPageModern.html")]
        public async Task GetNotificationsAsync_GetsFavoritesAndJournals(string fileName)
        {
            var credentials = GenerateCredentials();

            var httpMessageHandler = new MockHttpMessageHandler(
                new MockHttpTransaction(
                    HttpMethod.Get,
                    new("https://www.furaffinity.net/msg/others/"),
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            GetHtmlFromResource(fileName),
                            MediaTypeHeaderValue.Parse("text/html"))
                    },
                    GetHeaderChecks(credentials)));

            IFurAffinityClient client = new FurAffinityClient(
                httpMessageHandler,
                Domain.WWW,
                credentials);

            var result = await client.GetNotificationsAsync(CancellationToken.None);

            Assert.AreEqual(
                actual: result,
                expected: [
                    new Notification(
                        time: new(2026, 3, 19, 0, 54, 49, TimeSpan.Zero),
                        text: "Username1 faved Submission Title Here",
                        journalId: null),
                    new Notification(
                        time: new(2026, 3, 19, 21, 11, 56, TimeSpan.Zero),
                        text: "Title of Journal Here  (G)  posted by Username2",
                        journalId: 654321)
                ]);
        }

        [TestMethod]
        public void PostJournalAsync_FollowsProcess() =>
            Assert.Inconclusive("No unit tests implemented for write operations");
    }
}
