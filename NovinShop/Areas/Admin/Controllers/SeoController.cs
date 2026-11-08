using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovinShop.Data;
using NovinShop.Models;

namespace NovinShop.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = AppRoles.CatalogStaff)]
public class SeoController : Controller
{
    private readonly AppDbContext _db;
    public SeoController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() => View(new SeoReport(
        await _db.Products.AsNoTracking().Where(p => p.IsActive && (p.SeoDescription == null || p.SeoDescription == ""))
            .Select(p => new SeoItem(p.Id, p.Name)).ToListAsync(),
        await _db.Products.AsNoTracking().Where(p => p.IsActive && (p.ImagePath == null || p.ImagePath == ""))
            .Select(p => new SeoItem(p.Id, p.Name)).ToListAsync(),
        await _db.Categories.AsNoTracking().Where(c => c.IsActive && (c.SeoDescription == null || c.SeoDescription == ""))
            .Select(c => new SeoItem(c.Id, c.Name)).ToListAsync(),
        await _db.ContentPages.AsNoTracking().Where(p => p.IsActive && (p.SeoDescription == null || p.SeoDescription == ""))
            .Select(p => new SeoItem(p.Id, p.Title)).ToListAsync(),
        await _db.SlugRedirects.AsNoTracking().OrderByDescending(r => r.Id).Take(100).ToListAsync()));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRedirect(int id)
    {
        var r = await _db.SlugRedirects.FindAsync(id);
        if (r is not null) { _db.SlugRedirects.Remove(r); await _db.SaveChangesAsync(); }
        TempData["Ok"] = "ریدایرکت حذف شد";
        return RedirectToAction(nameof(Index));
    }
}
