using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovinShop.Data;
using NovinShop.Services;

namespace NovinShop.Controllers;

public class ContentController : Controller
{
    private readonly AppDbContext _db;
    public ContentController(AppDbContext db) => _db = db;

    [HttpGet("page/{slug}")]
    public async Task<IActionResult> Show(string slug)
    {
        var p = await _db.ContentPages.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive);
        if (p is null)
        {
            var r = await SlugRedirects.FindAsync(_db, "page", slug);
            return r is null ? NotFound() : RedirectToActionPermanent(nameof(Show), new { slug = r.NewSlug });
        }
        ViewData["Title"] = p.SeoTitle ?? $"{p.Title} | نوین‌شاپ";
        ViewData["Description"] = p.SeoDescription ?? p.Title;
        return View(p);
    }
}
