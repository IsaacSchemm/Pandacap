using DeviantArtFs.Extensions;

namespace Pandacap.HighLevel
{
    public class DeviantArtNotificationsHandler(
        DeviantArtCredentialProvider deviantArtCredentialProvider,
        DeviantArtLastVisitFinder deviantArtLastVisitFinder)
    {
        public record Message(
            DeviantArtFs.Api.Messages.Message Details,
            DateTimeOffset LastRead)
        {
            public string Type => Details.type;
            public string? Username => Details.originator.OrNull()?.username;
            public DateTimeOffset Timestamp => Details.ts.OrNull() ?? DateTimeOffset.MinValue;
            public Guid? DeviationId => Details.subject.Value.deviation.OrNull()?.deviationid;
            public string? Title => Details.subject.Value.deviation.OrNull()?.title?.OrNull();
            public bool IsNew => Details.is_new || LastRead < Timestamp;
        }

        public async IAsyncEnumerable<Message> GetNotificationsAsync()
        {
            if (await deviantArtCredentialProvider.GetCredentialsAsync() is not (var credentials, _))
                yield break;

            DateTimeOffset lastvisit = await deviantArtLastVisitFinder.FindMyLastVisitAsync()
                ?? DateTimeOffset.UtcNow;

            var feed = DeviantArtFs.Api.Messages.GetFeedAsync(
                credentials,
                DeviantArtFs.Api.Messages.StackMessages.Default,
                DeviantArtFs.Api.Messages.MessageFolder.Inbox,
                DeviantArtFs.Api.Messages.MessageCursor.Default);

            await foreach (var message in feed)
                yield return new(message, lastvisit);
        }
    }
}
