using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Pandacap.Database;
using Pandacap.Models;
using Pandacap.UI.Elements;
using System.Runtime.CompilerServices;

namespace Pandacap.Controllers
{
    public class CanonicalTagsController(
        PandacapDbContext pandacapDbContext) : Controller
    {
        private IEnumerable<Guid> GetPinnedPostIds(CanonicalCharacter canonicalCharacter)
        {
            foreach (var pinnedPostId in canonicalCharacter.PinnedPostIds)
                if (Guid.TryParse(pinnedPostId, out Guid guid))
                    yield return guid;
        }

        private IEnumerable<Guid> GetSpeciesIds(CanonicalCharacter canonicalCharacter)
        {
            if (canonicalCharacter.SpeciesId is Guid guid)
                yield return guid;
        }

        private async IAsyncEnumerable<CanonicalTagsViewModel.Character> GetCharactersAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var canonicalCharacters = await pandacapDbContext.CanonicalCharacters
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);

            var allPinnedPostIds = canonicalCharacters
                .SelectMany(GetPinnedPostIds)
                .ToHashSet();

            var allPinnedPosts = await pandacapDbContext.Posts
                .Where(p => allPinnedPostIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            var allSpeciesIds = canonicalCharacters
                .SelectMany(GetSpeciesIds)
                .ToHashSet();

            var speciesNames = await pandacapDbContext.CanonicalSpecies
                .Where(s => allSpeciesIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

            foreach (var cc in canonicalCharacters)
            {
                var query = pandacapDbContext.Posts
                    .Where(p => p.CharacterAppearances.Any(c =>
                        c.CharacterId == cc.Id && !c.Background))
                    .OrderByDescending(p => p.PublishedTime);

                var pinnedPostThumbnail = GetPinnedPostIds(cc)
                    .SelectMany(id => allPinnedPosts.Where(p => p.Id == id))
                    .OfType<IPost>()
                    .SelectMany(p => p.Thumbnails)
                    .FirstOrDefault();

                yield return new()
                {
                    Id = cc.Id,
                    Name = cc.Name,
                    SpeciesName = cc.SpeciesId is Guid speciesId && speciesNames.TryGetValue(speciesId, out string? speciesName)
                        ? speciesName
                        : null,
                    Original = cc.Original,
                    Fan = cc.Fan,
                    SettingId = cc.SettingId,
                    Thumbnail = pinnedPostThumbnail
                };
            }
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        //=> (await memoryCache.GetOrCreateAsync(
        //$"{nameof(CanonicalTagsController)}/Index",
        //async _ =>
        {
            var characters = await GetCharactersAsync(cancellationToken).ToListAsync(cancellationToken);

            var settings = await pandacapDbContext.CanonicalSettings
                .AsAsyncEnumerable()
                .Select(setting => new CanonicalTagsViewModel.Setting
                {
                    Id = setting.Id,
                    Name = setting.Name,
                    Characters = [
                        .. characters.Where(c => c.SettingId == setting.Id)
                    ]
                })
                .ToListAsync(cancellationToken);

            var species = await pandacapDbContext.CanonicalSpecies
                .OrderBy(s => s.Name)
                .ToListAsync(cancellationToken);

            return View(new CanonicalTagsViewModel
            {
                Settings = [
                    .. settings.OrderByDescending(s => s.Characters.Length),
                    new()
                    {
                        Id = null,
                        Name = "Other",
                        Characters = [
                            .. characters.Where(c => c.SettingId == null)
                        ]
                    }
                ],
                DisplayedSpecies = [
                    .. await pandacapDbContext.CanonicalSpecies
                        .OrderBy(s => s.Name)
                        .Select(s => new CanonicalTagsViewModel.Species
                        {
                            Id = s.Id,
                            Name = s.Name,
                            Original = s.Original,
                            Fan = s.Fan
                        })
                        .ToListAsync(cancellationToken)
                ],
                Mediums = [
                    .. await pandacapDbContext.CanonicalMediums
                        .OrderBy(s => s.Name)
                        .Select(s => new CanonicalTagsViewModel.Medium
                        {
                            Id = s.Id,
                            Name = s.Name
                        })
                        .ToListAsync(cancellationToken)
                ]
            });
        }
        //,
        //new MemoryCacheEntryOptions
        //{
        //    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        //}))!;

        private async IAsyncEnumerable<Post> GetPostsForCharacterAsync(
            CanonicalCharacter canonicalCharacter)
        {
            var pinnedPostIds = GetPinnedPostIds(canonicalCharacter).ToHashSet();

            var pinnedPosts = pandacapDbContext.Posts
                .Where(p => pinnedPostIds.Contains(p.Id))
                .AsAsyncEnumerable();

            await foreach (var post in pinnedPosts)
                yield return post;

            var typicalSpeciesPosts = pandacapDbContext.Posts
                .Where(p => p.CharacterAppearances.Any(c =>
                    c.CharacterId == canonicalCharacter.Id
                    && (c.SpeciesId == canonicalCharacter.SpeciesId || c.SpeciesId == null)
                    && !c.Background))
                .OrderByDescending(p => p.PublishedTime)
                .AsAsyncEnumerable();

            await foreach (var post in typicalSpeciesPosts)
                yield return post;

            var otherPosts = pandacapDbContext.Posts
                .Where(p => p.CharacterAppearances.Any(c =>
                    c.CharacterId == canonicalCharacter.Id
                    && !c.Background))
                .OrderByDescending(p => p.PublishedTime)
                .AsAsyncEnumerable();

            await foreach (var post in otherPosts)
                yield return post;
        }

        public async Task<IActionResult> Character(Guid id, CancellationToken cancellationToken)
        //=> (await memoryCache.GetOrCreateAsync(
        //    $"{nameof(CanonicalTagsController)}/Character/{id}",
        //    async _ =>
        {
            var character = await pandacapDbContext.CanonicalCharacters
                .Where(c => c.Id == id)
                .SingleAsync(cancellationToken);

            var posts = await GetPostsForCharacterAsync(character)
                .DistinctBy(p => p.Id)
                .Take(4)
                .OrderByDescending(p => p.PublishedTime)
                .ToListAsync(cancellationToken);

            IEnumerable<Guid> otherCharacterIds = [
                .. character.Relationships.Select(r => r.OtherCharacterId),
                .. character.AlternateVersions.Select(r => r.OtherCharacterId)
            ];

            var otherCharacters = await pandacapDbContext.CanonicalCharacters
                .Where(c => otherCharacterIds.Contains(c.Id))
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.SettingId
                })
                .ToListAsync(cancellationToken);

            var otherSettingIds = otherCharacters.Select(c => c.SettingId ?? Guid.Empty);

            var otherSettings = await pandacapDbContext.CanonicalSettings
                .Where(s => otherSettingIds.Contains(s.Id))
                .ToListAsync(cancellationToken);

            return View(new CharacterTagModel
            {
                CanonicalCharacter = character,
                SpeciesName = await pandacapDbContext.CanonicalSpecies
                    .Where(x => x.Id == character.SpeciesId)
                    .Select(x => x.Name)
                    .FirstOrDefaultAsync(cancellationToken),
                SettingName = await pandacapDbContext.CanonicalSettings
                    .Where(x => x.Id == character.SettingId)
                    .Select(x => x.Name)
                    .FirstOrDefaultAsync(cancellationToken),
                Relationships = [
                    .. character.Relationships.Select(r => new CharacterTagModel.Relationship
                    {
                        CharacterId = r.OtherCharacterId,
                        CharacterName = (from o in otherCharacters
                                            where o.Id == r.OtherCharacterId
                                            select o.Name).FirstOrDefault(),
                        RelationshipTypeName = r.RelationshipTypeName
                    })
                ],
                AlternateVersions = [
                    .. character.AlternateVersions.Select(a => new CharacterTagModel.AlternateVersion
                    {
                        CharacterId = a.OtherCharacterId,
                        CharacterName = (from o in otherCharacters
                                            where o.Id == a.OtherCharacterId
                                            select o.Name).FirstOrDefault(),
                        SettingName = (from o in otherCharacters
                                        join s in otherSettings on o.SettingId equals s.Id
                                        where o.Id == a.OtherCharacterId
                                        select s.Name).FirstOrDefault()
                    })
                ],
                Posts = posts
            });
        }
        //,
        //new MemoryCacheEntryOptions
        //{
        //    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        //}))!;
    }
}
