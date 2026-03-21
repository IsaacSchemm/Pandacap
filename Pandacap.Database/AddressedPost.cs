using Microsoft.FSharp.Collections;
using Pandacap.Text;
using Pandacap.UI.Badges;
using Pandacap.UI.Elements;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pandacap.Database
{
    public class AddressedPost : IPost
    {
        public Guid Id { get; set; }

        public string? InReplyTo { get; set; }

        public string? Community { get; set; }

        public List<string> Users { get; set; } = [];

        public DateTimeOffset PublishedTime { get; set; }

        public string? Title { get; set; }

        public string HtmlContent { get; set; } = "";

        public bool IsDirectMessage { get; set; }

        public string? BlueskyDID { get; set; }

        public string? BlueskyRecordKey { get; set; }

        [NotMapped]
        public bool IsReply => InReplyTo != null;

        private IEnumerable<string> EnumerateRecipients()
        {
            foreach (var user in Users)
                yield return user;

            if (Community is string c)
                yield return c;
        }

        public record PostAddressing(
            FSharpList<string> To,
            FSharpList<string> Cc);

        [NotMapped]
        public PostAddressing Addressing =>
            IsDirectMessage
                ? new(
                    To: [.. EnumerateRecipients()],
                    Cc: [])
            : IsReply
                ? new(
                    To: ["https://www.w3.org/ns/activitystreams#Public"],
                    Cc: [.. EnumerateRecipients()])
                : new(
                    To: ["https://www.w3.org/ns/activitystreams#Public"],
                    Cc: []);

        Badge IPost.Badge => Badges.ActivityPub;

        string IPost.DisplayTitle => !string.IsNullOrWhiteSpace(Title)
            ? Title
            : ExcerptGenerator.FromText(60, TextConverter.FromHtml(HtmlContent));

        string IPost.Id => $"{Id}";

        string IPost.InternalUrl => $"/AddressedPosts/{this.Id}";

        string IPost.ExternalUrl => $"/AddressedPosts/{this.Id}";

        DateTimeOffset IPost.PostedAt => PublishedTime;

        string IPost.ProfileUrl => null!;

        IEnumerable<IPostThumbnail> IPost.Thumbnails => [];

        string IPost.Username => null!;

        string IPost.Usericon => null!;
    }
}
