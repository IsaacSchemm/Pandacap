using DeviantArtFs.Extensions;

namespace Pandacap.HighLevel
{
    public class DeviantArtNotificationsHandler(
        DeviantArtCredentialProvider deviantArtCredentialProvider)
    {
        public record Message(DeviantArtFs.Api.Messages.Message Details)
        {
            public string Type => Details.type;
            public string? Username => Details.originator.OrNull()?.username;
            public DateTimeOffset Timestamp => Details.ts.OrNull() ?? DateTimeOffset.MinValue;
            public Guid? DeviationId => Details.subject.Value.deviation.OrNull()?.deviationid;
            public string? Title => Details.subject.Value.deviation.OrNull()?.title?.OrNull();
            public bool IsNew => Details.is_new;
        }

        public async IAsyncEnumerable<Message> GetNotificationsAsync()
        {
            if (await deviantArtCredentialProvider.GetCredentialsAsync() is not (var credentials, _))
                yield break;

            var feed = DeviantArtFs.Api.Messages.GetFeedAsync(
                credentials,
                DeviantArtFs.Api.Messages.StackMessages.Default,
                DeviantArtFs.Api.Messages.MessageFolder.Inbox,
                DeviantArtFs.Api.Messages.MessageCursor.Default);

            await foreach (var message in feed)
                yield return new(message);
        }
    }
}
