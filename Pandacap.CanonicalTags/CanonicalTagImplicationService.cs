using Microsoft.EntityFrameworkCore;
using Pandacap.CanonicalTags.Interfaces;
using Pandacap.Database;

namespace Pandacap.CanonicalTags
{
    internal class CanonicalTagImplicationService(
        PandacapDbContext pandacapDbContext) : ICanonicalTagImplicationService
    {
        private readonly Lazy<Task<IReadOnlyList<CanonicalCharacter>>> AllCharacters = new(async () =>
            await pandacapDbContext.CanonicalCharacters.ToListAsync());

        private readonly Lazy<Task<IReadOnlyList<CanonicalSpecies>>> AllSpecies = new(async () =>
            await pandacapDbContext.CanonicalSpecies.ToListAsync());

        public async IAsyncEnumerable<Guid> GetImplicitTagsAsync(Post post)
        {
            foreach (var a in post.MediumApplications)
                yield return a.MediumId;

            foreach (var a in post.CharacterAppearances)
            {
                yield return a.CharacterId;

                foreach (var character in (await AllCharacters.Value).Where(c => c.Id == a.CharacterId))
                {
                    if (character.SettingId is Guid settingId)
                        yield return settingId;

                    var depictedSpeciesId = a.SpeciesId ?? character.SpeciesId;

                    if (depictedSpeciesId is Guid speciesId)
                        await foreach (var impliedSpeciesId in GetGeneralSpeciesAsync(speciesId))
                            yield return impliedSpeciesId;
                }
            }
        }

        private async IAsyncEnumerable<Guid> GetGeneralSpeciesAsync(Guid specificSpeciesId, int depth = 5)
        {
            yield return specificSpeciesId;

            if (depth <= 0)
                yield break;

            var matches = (await AllSpecies.Value)
                .Where(s => s.Id == specificSpeciesId);

            foreach (var species in matches)
                foreach (var relationship in species.PartOf)
                    await foreach (var id in GetGeneralSpeciesAsync(relationship.OtherSpeciesId, depth - 1))
                        yield return id;
        }
    }
}
