using Microsoft.FSharp.Collections;

namespace Pandacap.Models
{
    public record CanonicalCharacterOrSpeciesModel
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public required bool Original { get; init; }
        public required bool Fan { get; init; }
        public FSharpList<string> NationalityIsoCodes { get; init; } = [];

        public IEnumerable<string> NationalityFlagEmoji =>
            NationalityIsoCodes.Select(
                code => string.Concat(
                    code.Select(ch =>
                        char.ConvertFromUtf32(ch + 0x1F1A5))));
    }

    public record CanonicalCharacterAppearanceModel
    {
        public required CanonicalCharacterOrSpeciesModel? Character { get; init; }
        public required CanonicalCharacterOrSpeciesModel? Species { get; init; }
        public required bool Background { get; init; }
    }
}
