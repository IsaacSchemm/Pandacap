using Pandacap.Database;
using Pandacap.UI.Badges;
using Pandacap.UI.Elements;

namespace Pandacap.Models
{
    public class CharacterTagModel
    {
        public required CanonicalCharacter CanonicalCharacter { get; init; }

        public required string? SpeciesName { get; init; }
        public required string? SettingName { get; init; }

        public required IReadOnlyList<Post> Posts { get; init; }
    }
}
