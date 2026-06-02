using Pandacap.Database;

namespace Pandacap.Models
{
    public class CharacterTagModel
    {
        public required CanonicalCharacter CanonicalCharacter { get; init; }

        public required string? SpeciesName { get; init; }
        public required string? SettingName { get; init; }

        public class Relationship
        {
            public required Guid CharacterId { get; init; }
            public required string? CharacterName { get; init; }
            public required string RelationshipTypeName { get; init; }
        }

        public required IReadOnlyList<Relationship> Relationships { get; init; }

        public class AlternateVersion
        {
            public required Guid CharacterId { get; init; }
            public required string? CharacterName { get; init; }
            public required string? SettingName { get; init; }
        }

        public required IReadOnlyList<AlternateVersion> AlternateVersions { get; init; }

        public required IReadOnlyList<Post> Posts { get; init; }
    }
}
