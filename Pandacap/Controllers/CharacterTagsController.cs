using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    public class CharacterTagsController(
        PandacapDbContext pandacapDbContext) : Controller
    {
        public async Task<IActionResult> Index(Guid id, CancellationToken cancellationToken)
        {
            var character = await pandacapDbContext.CanonicalCharacters
                .Where(c => c.Id == id)
                .SingleAsync(cancellationToken);

            var posts = await pandacapDbContext.Posts
                .Where(p => p.CharacterAppearances.Any(c =>
                    c.CharacterId == id
                    && !c.Background))
                .OrderByDescending(p => p.PublishedTime)
                .Take(4)
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
    }
}
