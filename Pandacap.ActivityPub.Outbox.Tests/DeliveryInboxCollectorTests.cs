using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub.Outbox.Interfaces;
using Pandacap.Database;

namespace Pandacap.ActivityPub.Outbox.Tests
{
    [TestClass]
    public sealed class DeliveryInboxCollectorTests
    {
        [TestMethod]
        public async Task GetDeliveryInboxesAsync_CollectsInboxes()
        {
            using var context = new PandacapDbContext(
                new DbContextOptionsBuilder<PandacapDbContext>()
                .UseInMemoryDatabase("Pandacap.ActivityPub.Outbox.Tests")
                .Options);

            IDeliveryInboxCollector collector = new TestCollector(
                followers: [
                    new()
                    {
                        Inbox = "https://www.example.com/inbox1"
                    },
                    new()
                    {
                        Inbox = "https://www.example.com/inbox2"
                    }],
                follows: [new()
                    {
                        Inbox = "https://www.example.com/inbox99"
                    }]);

            var actual = await collector.GetDeliveryInboxesAsync(
                isCreate: true,
                cancellationToken: CancellationToken.None);

            Assert.AreEqual(
                [
                    "https://www.example.com/inbox1",
                    "https://www.example.com/inbox2"
                ],
                actual);
        }

        [TestMethod]
        public async Task GetDeliveryInboxesAsync_IncludesSharedInboxesWhenAvailable()
        {
            using var context = new PandacapDbContext(
                new DbContextOptionsBuilder<PandacapDbContext>()
                .UseInMemoryDatabase("Pandacap.ActivityPub.Outbox.Tests")
                .Options);

            IDeliveryInboxCollector collector = new TestCollector(
                followers: [
                    new()
                    {
                        Inbox = "https://www.example.com/inbox1"
                    },
                    new()
                    {
                        Inbox = "https://www.example.com/inbox2",
                        SharedInbox = "https://www.example.com/inbox3"
                    }],
                follows: [new()
                    {
                        Inbox = "https://www.example.com/inbox99"
                    }]);

            var actual = await collector.GetDeliveryInboxesAsync(
                isCreate: true,
                cancellationToken: CancellationToken.None);

            Assert.AreEqual(
                [
                    "https://www.example.com/inbox1",
                    "https://www.example.com/inbox3"
                ],
                actual);
        }

        [TestMethod]
        public async Task GetDeliveryInboxesAsync_MoreExpansiveForUpdates()
        {
            using var context = new PandacapDbContext(
                new DbContextOptionsBuilder<PandacapDbContext>()
                .UseInMemoryDatabase("Pandacap.ActivityPub.Outbox.Tests")
                .Options);

            IDeliveryInboxCollector collector = new TestCollector(
                followers: [
                    new()
                    {
                        Inbox = "https://www.example.com/inbox1"
                    },
                    new()
                    {
                        Inbox = "https://www.example.com/inbox2"
                    }],
                follows: [new()
                    {
                        Inbox = "https://www.example.com/inbox99"
                    }]);

            var actual = await collector.GetDeliveryInboxesAsync(
                isCreate: false,
                cancellationToken: CancellationToken.None);

            Assert.AreEqual(
                [
                    "https://www.example.com/inbox1",
                    "https://www.example.com/inbox2",
                    "https://www.example.com/inbox99"
                ],
                actual);
        }

        private class TestCollector(
            IReadOnlyList<Follower> followers,
            IReadOnlyList<Follow> follows) : DeliveryInboxCollector(null!)
        {
            internal override IAsyncEnumerable<Follower> Followers => followers.ToAsyncEnumerable();
            internal override IAsyncEnumerable<Follow> Follows => follows.ToAsyncEnumerable();
        }
    }
}
