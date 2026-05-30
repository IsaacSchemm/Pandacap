using Microsoft.EntityFrameworkCore;
using Pandacap.CanonicalTags.ShortCodes.Interfaces;
using Pandacap.Database;
using System.Text;

namespace Pandacap.CanonicalTags.ShortCodes
{
    internal class CanonicalTagShortCodeService(
        PandacapDbContext pandacapDbContext) : ICanonicalTagShortCodeService
    {
        private readonly Lazy<Task<IReadOnlyList<CanonicalMedium>>> _mediums = new(async () =>
            await pandacapDbContext.CanonicalMediums.ToListAsync());

        private readonly Lazy<Task<IReadOnlyList<CanonicalCharacter>>> _characters = new(async () =>
            await pandacapDbContext.CanonicalCharacters.ToListAsync());

        private readonly Lazy<Task<IReadOnlyList<CanonicalSpecies>>> _species = new(async () =>
            await pandacapDbContext.CanonicalSpecies.ToListAsync());

        public async Task ApplyCanonicalTagsUsingShortCodesAsync(
            Guid postId,
            IEnumerable<string> shortCodes,
            CancellationToken cancellationToken)
        {
            var applied = await GetShortCodesForAttachedCanonicalTagsAsync(postId).ToListAsync(cancellationToken);

            foreach (var shortCode in shortCodes.Except(applied))
            {
                var parts = shortCode.TrimStart('/').Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length == 0)
                    continue;

                var primary = parts[0];
                var additional = parts.Skip(1);

                var found = 0;

                foreach (var medium in await _mediums.Value)
                {
                    if ((medium.ShortCode ?? $"{medium.Id}") == primary)
                    {
                        pandacapDbContext.CanonicalMediumApplications.Add(new()
                        {
                            Id = Guid.NewGuid(),
                            PostId = postId,
                            MediumId = medium.Id
                        });

                        found++;
                    }
                }

                foreach (var character in await _characters.Value)
                {
                    if ((character.ShortCode ?? $"{character.Id}") == primary)
                    {
                        Guid? speciesId = null;

                        foreach (var species in await _species.Value)
                            if (additional.Contains(species.ShortCode ?? $"{species.Id}"))
                                speciesId = species.Id;

                        pandacapDbContext.CanonicalCharacterAppearances.Add(new()
                        {
                            Id = Guid.NewGuid(),
                            PostId = postId,
                            CharacterId = character.Id,
                            SpeciesId = speciesId,
                            Background = shortCode.StartsWith('/')
                        });

                        found++;
                    }
                }

                if (found == 0)
                    throw new Exception($"No match found for short code {shortCode}");

                await pandacapDbContext.SaveChangesAsync(cancellationToken);
            }
        }

        public async IAsyncEnumerable<string> GetShortCodesForAttachedCanonicalTagsAsync(
            Guid postId)
        {
            await foreach (var application in pandacapDbContext.CanonicalMediumApplications
                .Where(a => a.PostId == postId)
                .AsAsyncEnumerable())
            {
                foreach (var medium in await _mediums.Value)
                    if (medium.Id == application.MediumId)
                        yield return medium.ShortCode ?? $"{medium.Id}";
            }

            await foreach (var appearance in pandacapDbContext.CanonicalCharacterAppearances
                .Where(a => a.PostId == postId)
                .AsAsyncEnumerable())
            {
                foreach (var character in await _characters.Value)
                {
                    if (character.Id == appearance.CharacterId)
                    {
                        var sb = new StringBuilder();

                        if (appearance.Background)
                            sb.Append('/');

                        sb.Append(character.ShortCode ?? $"{character.Id}");

                        foreach (var species in await _species.Value)
                            if (species.Id == appearance.SpeciesId)
                                sb.Append($".{species.ShortCode ?? $"{species.Id}"}");

                        yield return sb.ToString();
                    }
                }
            }
        }
    }
}
