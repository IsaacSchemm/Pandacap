using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.CanonicalTags.Interfaces;
using Pandacap.Database;
using Pandacap.Models;
using System.Text.Json;

namespace Pandacap.Controllers
{
    [Authorize]
    public class CanonicalTagsController(
        ICanonicalTagTreeService canonicalTagTreeService,
        PandacapDbContext pandacapDbContext) : Controller
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            WriteIndented = true
        };

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var model = new CanonicalTagsViewModel
            {
                ArtMedia = await canonicalTagTreeService.GetAllMediumsAsync().ToListAsync(cancellationToken),
                Characters = await canonicalTagTreeService.GetAllCharactersAsync().ToListAsync(cancellationToken),
                Settings = await canonicalTagTreeService.GetAllSettingsAsync().ToListAsync(cancellationToken),
                Species = await canonicalTagTreeService.GetAllSpeciesAsync().ToListAsync(cancellationToken)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> AddEditMedium(
            Guid? id,
            CancellationToken cancellationToken)
        {
            var existingMedium = await pandacapDbContext.CanonicalMediums
                .Where(m => m.Id == id)
                .FirstOrDefaultAsync(cancellationToken);

            return View("EditJson", new EditJsonViewModel
            {
                Json = JsonSerializer.Serialize(
                    existingMedium ?? new()
                    {
                        Id = Guid.NewGuid()
                    },
                    _jsonSerializerOptions)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEditMedium(
            EditJsonViewModel model,
            CancellationToken cancellationToken)
        {
            var newMedium = JsonSerializer.Deserialize<CanonicalMedium>(model.Json);
            if (newMedium == null)
                return BadRequest();

            var existingMedium = await pandacapDbContext.CanonicalMediums
                .Where(m => m.Id == newMedium.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (model.Delete)
            {
                pandacapDbContext.CanonicalMediums.Remove(
                    existingMedium ?? throw new Exception("Item to delete not found"));

                await pandacapDbContext.SaveChangesAsync(cancellationToken);

                return RedirectToAction(nameof(Index));
            }

            if (existingMedium == null)
            {
                existingMedium = new() { Id = newMedium.Id };
                pandacapDbContext.CanonicalMediums.Add(existingMedium);
            }

            existingMedium.Name = newMedium.Name;
            existingMedium.ShortCode = newMedium.ShortCode;

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> AddEditCharacter(
            Guid? id,
            CancellationToken cancellationToken)
        {
            var existing = await pandacapDbContext.CanonicalCharacters
                .Where(m => m.Id == id)
                .FirstOrDefaultAsync(cancellationToken);

            return View("EditJson", new EditJsonViewModel
            {
                Json = JsonSerializer.Serialize(
                    existing ?? new()
                    {
                        Id = Guid.NewGuid(),
                        NationalityIsoCodes = ["ZZ"],
                        Relationships = [new()],
                        AlternateVersions = [new()]
                    },
                    _jsonSerializerOptions)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEditCharacter(
            EditJsonViewModel model,
            CancellationToken cancellationToken)
        {
            var newItem = JsonSerializer.Deserialize<CanonicalCharacter>(model.Json);
            if (newItem == null)
                return BadRequest();

            var existingItem = await pandacapDbContext.CanonicalCharacters
                .Where(m => m.Id == newItem.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (model.Delete)
            {
                pandacapDbContext.Remove(
                    existingItem ?? throw new Exception("Item to delete not found"));

                await pandacapDbContext.SaveChangesAsync(cancellationToken);

                return RedirectToAction(nameof(Index));
            }

            if (existingItem == null)
            {
                existingItem = new() { Id = newItem.Id };
                pandacapDbContext.Add(existingItem);
            }

            existingItem.Name = newItem.Name;
            existingItem.FullName = newItem.FullName;
            existingItem.SpeciesId = newItem.SpeciesId;
            existingItem.SettingId = newItem.SettingId;
            existingItem.Gender = newItem.Gender;
            existingItem.Pronouns = newItem.Pronouns;
            existingItem.NationalityIsoCodes = newItem.NationalityIsoCodes;
            existingItem.Description = newItem.Description;
            existingItem.Relationships = newItem.Relationships;
            existingItem.AlternateVersions = newItem.AlternateVersions;
            existingItem.ShortCode = newItem.ShortCode;

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> AddEditSetting(
            Guid? id,
            CancellationToken cancellationToken)
        {
            var existing = await pandacapDbContext.CanonicalSettings
                .Where(m => m.Id == id)
                .FirstOrDefaultAsync(cancellationToken);

            return View("EditJson", new EditJsonViewModel
            {
                Json = JsonSerializer.Serialize(
                    existing ?? new()
                    {
                        Id = Guid.NewGuid()
                    },
                    _jsonSerializerOptions)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEditSetting(
            EditJsonViewModel model,
            CancellationToken cancellationToken)
        {
            var newItem = JsonSerializer.Deserialize<CanonicalSetting>(model.Json);
            if (newItem == null)
                return BadRequest();

            var existingItem = await pandacapDbContext.CanonicalSettings
                .Where(m => m.Id == newItem.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (model.Delete)
            {
                pandacapDbContext.Remove(
                    existingItem ?? throw new Exception("Item to delete not found"));

                await pandacapDbContext.SaveChangesAsync(cancellationToken);

                return RedirectToAction(nameof(Index));
            }

            if (existingItem == null)
            {
                existingItem = new() { Id = newItem.Id };
                pandacapDbContext.Add(existingItem);
            }

            existingItem.Name = newItem.Name;

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> AddEditSpecies(
            Guid? id,
            CancellationToken cancellationToken)
        {
            var existing = await pandacapDbContext.CanonicalSpecies
                .Where(m => m.Id == id)
                .FirstOrDefaultAsync(cancellationToken);

            return View("EditJson", new EditJsonViewModel
            {
                Json = JsonSerializer.Serialize(
                    existing ?? new()
                    {
                        Id = Guid.NewGuid(),
                        PartOf = [new()]
                    },
                    _jsonSerializerOptions)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEditSpecies(
            EditJsonViewModel model,
            CancellationToken cancellationToken)
        {
            var newItem = JsonSerializer.Deserialize<CanonicalSpecies>(model.Json);
            if (newItem == null)
                return BadRequest();

            var existingItem = await pandacapDbContext.CanonicalSpecies
                .Where(m => m.Id == newItem.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (model.Delete)
            {
                pandacapDbContext.Remove(
                    existingItem ?? throw new Exception("Item to delete not found"));

                await pandacapDbContext.SaveChangesAsync(cancellationToken);

                return RedirectToAction(nameof(Index));
            }

            if (existingItem == null)
            {
                existingItem = new() { Id = newItem.Id };
                pandacapDbContext.Add(existingItem);
            }

            existingItem.Name = newItem.Name;
            existingItem.Description = newItem.Description;
            existingItem.Original = newItem.Original;
            existingItem.Fan = newItem.Fan;
            existingItem.PartOf = newItem.PartOf;
            existingItem.ShortCode = newItem.ShortCode;

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(Index));
        }
    }
}
