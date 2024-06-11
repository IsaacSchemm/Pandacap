using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;

namespace Pandacap.Controllers
{
    [Authorize]
    public class ProfilePropertiesController : Controller
    {
        private readonly PandacapDbContext _context;

        public ProfilePropertiesController(PandacapDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.ProfileProperties.ToListAsync());
        }

        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
                return NotFound();

            var profileProperty = await _context.ProfileProperties
                .FirstOrDefaultAsync(m => m.Id == id);
            if (profileProperty == null)
                return NotFound();

            return View(profileProperty);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Value,Link")] ProfileProperty profileProperty)
        {
            if (ModelState.IsValid)
            {
                profileProperty.Id = Guid.NewGuid();
                _context.Add(profileProperty);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(profileProperty);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
                return NotFound();

            var profileProperty = await _context.ProfileProperties.FindAsync(id);
            if (profileProperty == null)
                return NotFound();

            return View(profileProperty);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,Value,Link")] ProfileProperty profileProperty)
        {
            if (id != profileProperty.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(profileProperty);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(profileProperty);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
                return NotFound();

            var profileProperty = await _context.ProfileProperties
                .FirstOrDefaultAsync(m => m.Id == id);
            if (profileProperty == null)
                return NotFound();

            return View(profileProperty);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var profileProperty = await _context.ProfileProperties.FindAsync(id);
            if (profileProperty != null)
                _context.ProfileProperties.Remove(profileProperty);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
