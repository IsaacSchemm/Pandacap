using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;

namespace Pandacap.Controllers
{
    public class ProfilePropertiesController : Controller
    {
        private readonly PandacapDbContext _context;

        public ProfilePropertiesController(PandacapDbContext context)
        {
            _context = context;
        }

        // GET: ProfileProperties
        public async Task<IActionResult> Index()
        {
            return View(await _context.ProfileProperties.ToListAsync());
        }

        // GET: ProfileProperties/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var profileProperty = await _context.ProfileProperties
                .FirstOrDefaultAsync(m => m.Id == id);
            if (profileProperty == null)
            {
                return NotFound();
            }

            return View(profileProperty);
        }

        // GET: ProfileProperties/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ProfileProperties/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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

        // GET: ProfileProperties/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var profileProperty = await _context.ProfileProperties.FindAsync(id);
            if (profileProperty == null)
            {
                return NotFound();
            }
            return View(profileProperty);
        }

        // POST: ProfileProperties/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,Value,Link")] ProfileProperty profileProperty)
        {
            if (id != profileProperty.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(profileProperty);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProfilePropertyExists(profileProperty.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(profileProperty);
        }

        // GET: ProfileProperties/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var profileProperty = await _context.ProfileProperties
                .FirstOrDefaultAsync(m => m.Id == id);
            if (profileProperty == null)
            {
                return NotFound();
            }

            return View(profileProperty);
        }

        // POST: ProfileProperties/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var profileProperty = await _context.ProfileProperties.FindAsync(id);
            if (profileProperty != null)
            {
                _context.ProfileProperties.Remove(profileProperty);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProfilePropertyExists(Guid id)
        {
            return _context.ProfileProperties.Any(e => e.Id == id);
        }
    }
}
