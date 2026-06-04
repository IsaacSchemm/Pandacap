using Microsoft.FSharp.Collections;
using Pandacap.UI.Badges;
using Pandacap.UI.Elements;

namespace Pandacap.Models
{
    public class CanonicalTagsViewModel
    {
        public record Character : IPost
        {
            public required Guid Id { get; init; }

            public required string Name { get; init; }

            public required Guid? SettingId { get; init; }

            public required IPostThumbnail? Thumbnail { get; init; }

            public DateTimeOffset Timestamp { get; init; }

            Badge IPost.Badge => Badges.Pandacap;

            string IPost.DisplayTitle => Name;

            string IPost.Id => $"{Id}";

            string? IPost.InternalUrl => $"/CharacterTags/Index?id={Id}";

            string? IPost.ExternalUrl => $"/CharacterTags/Index?id={Id}";

            DateTimeOffset IPost.PostedAt => Timestamp;

            string? IPost.ProfileUrl => null;

            IEnumerable<IPostThumbnail> IPost.Thumbnails => Thumbnail != null
                ? [Thumbnail]
                : [];

            string? IPost.Username => null;

            string? IPost.Usericon => null;
        }

        public record Setting
        {
            public required string Name { get; init; }

            public required FSharpList<Character> Characters { get; init; }
        }

        public required FSharpList<Setting> Settings { get; init; }
    }
}
