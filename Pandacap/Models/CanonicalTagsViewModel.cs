using Microsoft.FSharp.Collections;
using Pandacap.UI.Elements;

namespace Pandacap.Models
{
    public class CanonicalTagsViewModel
    {
        public record Character
        {
            public required Guid Id { get; init; }

            public required string Name { get; init; }

            public required string? SpeciesName { get; init; }

            public required bool Original { get; init; }

            public required bool Fan { get; init; }

            public required Guid? SettingId { get; init; }

            public required IPostThumbnail? Thumbnail { get; init; }
        }

        public record Setting
        {
            public required Guid? Id { get; init; }

            public required string Name { get; init; }

            public required FSharpList<Character> Characters { get; init; }
        }

        public required FSharpList<Setting> Settings { get; init; }

        public record Species
        {
            public required Guid Id { get; init; }

            public required string Name { get; init; }

            public required bool Original { get; init; }

            public required bool Fan { get; init; }
        }

        public required FSharpList<Species> DisplayedSpecies { get; init; }

        public record Medium
        {
            public required Guid Id { get; init; }

            public required string Name { get; init; }
        }

        public required FSharpList<Medium> Mediums { get; init; }
    }
}
