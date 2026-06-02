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
                Posts = posts
            });
        }
    }
}
