using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiAreaPortal.Data;
using MultiAreaPortal.Models;

namespace MultiAreaPortal.Areas.Admin.Controllers;

// Full event CRUD using Entity Framework Core only. Admin role required.
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class EventsController(ApplicationDbContext context) : Controller
{
    // GET: /Admin/Events
    public async Task<IActionResult> Index()
    {
        var events = await context.Events
            .AsNoTracking()
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
        return View(events);
    }

    // GET: /Admin/Events/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var ev = await context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        return ev is null ? NotFound() : View(ev);
    }

    // GET: /Admin/Events/Create
    public IActionResult Create() => View(new Event());

    // POST: /Admin/Events/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Description,TicketPrice,EventDate")] Event model)
    {
        if (!ModelState.IsValid)
            return View(model);

        model.CreatedAt = DateTime.UtcNow;
        context.Events.Add(model);
        await context.SaveChangesAsync();
        TempData["Success"] = $"Event \"{model.Name}\" created.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Admin/Events/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var ev = await context.Events.FindAsync(id);
        return ev is null ? NotFound() : View(ev);
    }

    // POST: /Admin/Events/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,TicketPrice,EventDate,CreatedAt")] Event model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        context.Events.Update(model);
        await context.SaveChangesAsync();
        TempData["Success"] = $"Event \"{model.Name}\" updated.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Admin/Events/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        return ev is null ? NotFound() : View(ev);
    }

    // POST: /Admin/Events/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var ev = await context.Events.FindAsync(id);
        if (ev is not null)
        {
            context.Events.Remove(ev);
            await context.SaveChangesAsync();
            TempData["Success"] = $"Event \"{ev.Name}\" deleted.";
        }
        return RedirectToAction(nameof(Index));
    }
}
