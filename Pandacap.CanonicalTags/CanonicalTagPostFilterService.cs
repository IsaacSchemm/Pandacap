//using Microsoft.EntityFrameworkCore;
//using Pandacap.CanonicalTags.Interfaces;
//using Pandacap.Database;
//using System.Runtime.CompilerServices;

//namespace Pandacap.CanonicalTags
//{
//    internal class CanonicalTagPostFilterService(
//        PandacapDbContext pandacapDbContext) : ICanonicalTagPostFilterService
//    {
//        public IAsyncEnumerable<Guid> FindRelevantTagsAsync(Guid primaryTagId) => new[] { primaryTagId }
//            .ToAsyncEnumerable()
//            .SelectMany(AddChildSpeciesAsync)
//            .SelectMany(AddCharacterTagsForSpeciesAsync)
//            .SelectMany(AddCharacterTagsForSettingAsync);

//        public IQueryable<Post> FilterToPossibleMatches(IQueryable<Post> posts, IEnumerable<Guid> tagIds)=>
//            posts.Where(p =>
//                p.CharacterAppearances.Any(a => tagIds.Contains(a.CharacterId))
//                || p.CharacterAppearances.Any(a => a.SpeciesId != null && tagIds.Contains(a.SpeciesId.Value))
//                || p.MediumApplications.Any(a => tagIds.Contains(a.MediumId)));

//        public bool IsMatch(Post post)
//        {
//            throw new NotImplementedException();
//        }

//        private async IAsyncEnumerable<Guid> AddChildSpeciesAsync(Guid parentSpeciesId, int depth = 5)
//        {
//            yield return parentSpeciesId;

//            if (depth <= 0)
//                yield break;

//            var matches = pandacapDbContext.CanonicalSpecies
//                .Where(s => s.PartOf.Any(p => p.OtherSpeciesId == parentSpeciesId))
//                .Select(s => new { s.Id })
//                .AsAsyncEnumerable();

//            await foreach (var species in matches)
//                await foreach (var id in AddChildSpeciesAsync(species.Id, depth - 1))
//                    yield return id;
//        }

//        private async IAsyncEnumerable<Guid> AddCharacterTagsForSpeciesAsync(Guid speciesId)
//        {
//            yield return speciesId;

//            var matches = pandacapDbContext.CanonicalCharacters
//                .Where(c => c.SpeciesId == speciesId)
//                .Select(c => new { c.Id })
//                .AsAsyncEnumerable();

//            await foreach (var character in matches)
//                yield return character.Id;
//        }

//        private async IAsyncEnumerable<Guid> AddCharacterTagsForSettingAsync(Guid settingId)
//        {
//            yield return settingId;

//            var matches = pandacapDbContext.CanonicalCharacters
//                .Where(c => c.SettingId == settingId)
//                .Select(c => new { c.Id })
//                .AsAsyncEnumerable();

//            await foreach (var character in matches)
//                yield return character.Id;
//        }
//    }
//}
