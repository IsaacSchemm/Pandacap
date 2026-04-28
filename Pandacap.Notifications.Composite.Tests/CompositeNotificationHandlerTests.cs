using Microsoft.FSharp.Collections;
using Pandacap.Notifications.Composite.Interfaces;
using Pandacap.Notifications.Interfaces;
using Pandacap.UI.Badges;

namespace Pandacap.Notifications.Composite.Tests
{
    [TestClass]
    public sealed class CompositeNotificationHandlerTests
    {
        [TestMethod]
        public async Task GetNotificationsAsync_CombinesItemsNewestFirst()
        {
            var red = new Notification[]
            {
                new("2000-12-01"),
                new("2000-08-15"),
                new("2000-07-04")
            };

            var blue = new Notification[]
            {
                new("2001-01-02"),
                new("2000-11-01"),
                new("2000-02-28")
            };

            var green = new Notification[]
            {
                new("2005-11-11"),
                new("1994-10-31")
            };

            FSharpList<INotification> actual = [
                .. await GetCompositeNotificationHandler([
                    new Success(red),
                    new Success(green),
                    new Success(blue)
                ]).GetNotificationsAsync().ToListAsync(CancellationToken.None)
            ];

            FSharpList<INotification> expected = [
                new Notification("2005-11-11"),
                new Notification("2001-01-02"),
                new Notification("2000-12-01"),
                new Notification("2000-11-01"),
                new Notification("2000-08-15"),
                new Notification("2000-07-04"),
                new Notification("2000-02-28"),
                new Notification("1994-10-31")
            ];

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task GetNotificationsAsync_HandlesImmediateFailures()
        {
            var red = new Notification[]
            {
                new("2000-12-01"),
                new("2000-08-15"),
                new("2000-07-04")
            };

            FSharpList<INotification> actual = [
                .. await GetCompositeNotificationHandler([
                    new FailureAtEnd([]),
                    new Success(red),
                    new FailureAtEnd([])
                ]).GetNotificationsAsync().ToListAsync(CancellationToken.None)
            ];

            Assert.Contains(FailureAtEnd.FailureMessage, actual[0].UserName);
            Assert.Contains(FailureAtEnd.FailureMessage, actual[1].UserName);
            Assert.AreEqual(red[0], actual[2]);
            Assert.AreEqual(red[1], actual[3]);
            Assert.AreEqual(red[2], actual[4]);
            Assert.HasCount(5, actual);
        }

        [TestMethod]
        public async Task GetNotificationsAsync_HandlesFailuresAtEnd()
        {
            var red = new Notification[]
            {
                new("2000-12-01"),
                new("2000-08-15"),
                new("2000-07-04")
            };

            var blue = new Notification[]
            {
                new("2000-11-30")
            };

            FSharpList<INotification> actual = [
                .. await GetCompositeNotificationHandler([
                    new Success(red),
                    new FailureAtEnd(blue)
                ]).GetNotificationsAsync().ToListAsync(CancellationToken.None)
            ];

            Assert.Contains(red[2], actual);
            Assert.Contains(blue[0], actual);
            Assert.IsTrue(
                actual.Any(item => item.UserName.Contains(FailureAtEnd.FailureMessage)));
            Assert.HasCount(5, actual);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance", Justification = "Testing explicit interface implementation")]
        private ICompositeNotificationHandler GetCompositeNotificationHandler(IEnumerable<INotificationHandler> handlers) =>
            new CompositeNotificationHandler(handlers);

        private record Notification(string TimestampString) : INotification
        {
            public string ActivityName => throw new NotImplementedException();

            public Badge Badge => throw new NotImplementedException();

            public string Url => throw new NotImplementedException();

            public string UserName => $"Test User ({TimestampString})";

            public string UserUrl => throw new NotImplementedException();

            public string PostUrl => throw new NotImplementedException();

            public DateTimeOffset Timestamp => DateTimeOffset.Parse(TimestampString);
        }

        private class FailureAtEnd(IEnumerable<INotification> notifications) : INotificationHandler
        {
            public const string FailureMessage = "Expected Failure";

            public async IAsyncEnumerable<INotification> GetNotificationsAsync()
            {
                foreach (var item in notifications)
                {
                    await Task.Delay(TimeSpan.FromSeconds(0.1));
                    yield return item;
                }

                await Task.Delay(TimeSpan.FromSeconds(0.1));
                throw new Exception(FailureMessage);
            }
        }

        private class Success(IEnumerable<INotification> notifications) : INotificationHandler
        {
            public const string FailureMessage = "Expected Failure";

            public async IAsyncEnumerable<INotification> GetNotificationsAsync()
            {
                foreach (var item in notifications)
                {
                    await Task.Delay(TimeSpan.FromSeconds(0.1));
                    yield return item;
                }
            }
        }
    }
}
