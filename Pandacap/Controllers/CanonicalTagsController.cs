using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.Models;
using System.Text.Json;

namespace Pandacap.Controllers
{
    [Authorize]
    public class CanonicalTagsController(
        PandacapDbContext pandacapDbContext) : Controller
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public IActionResult Index()
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        public async Task<IActionResult> AddEditMedium(
            Guid? id,
            CancellationToken cancellationToken)
        {
            var existingMedium = await pandacapDbContext.CanonicalArtMedia
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
            var newMedium = JsonSerializer.Deserialize<CanonicalArtMedium>(model.Json);
            if (newMedium == null)
                return BadRequest();

            var existingMedium = await pandacapDbContext.CanonicalArtMedia
                .Where(m => m.Id == newMedium.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (model.Delete)
            {
                pandacapDbContext.CanonicalArtMedia.Remove(
                    existingMedium ?? throw new Exception("Item to delete not found"));

                await pandacapDbContext.SaveChangesAsync(cancellationToken);

                return RedirectToAction(nameof(Index));
            }

            if (existingMedium == null)
            {
                existingMedium = new() { Id = newMedium.Id };
                pandacapDbContext.CanonicalArtMedia.Add(existingMedium);
            }

            existingMedium.Name = newMedium.Name;
            existingMedium.ShortCode = newMedium.ShortCode;

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(AddEditMedium), new { id = existingMedium.Id });
        }
    }
}
