using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovinShop.Data;
using NovinShop.Models;
using NovinShop.Services;

namespace NovinShop.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = AppRoles.CatalogStaff)]
public class PagesController : Controller
{
    private const string Fields = "Title,Slug,Content,IsActive,ShowInFooter,SortOrder,SeoTitle,SeoDescription";
    private readonly AppDbContext _db;
    private readonly SiteSettings _site;
    public PagesController(AppDbContext db, SiteSettings site) { _db = db; _site = site; }

    public async Task<IActionResult> Index() =>
        View(await _db.ContentPages.AsNoTracking().OrderBy(p => p.SortOrder).ThenBy(p => p.Id).ToListAsync());

    public IActionResult Create() => View("Form", new ContentPage());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Fields)] ContentPage model)
    {
        await PrepareSlugAsync(model);
        if (!ModelState.IsValid) return View("Form", model);
        _db.ContentPages.Add(model);
        await _db.SaveChangesAsync();
        _site.Invalidate();
        TempData["Ok"] = "صفحه ثبت شد";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var p = await _db.ContentPages.FindAsync(id);
        return p is null ? NotFound() : View("Form", p);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind(Fields)] ContentPage model)
    {
        var p = await _db.ContentPages.FindAsync(id);
        if (p is null) return NotFound();
        model.Id = id;
        await PrepareSlugAsync(model);
        if (!ModelState.IsValid) return View("Form", model);

        await SlugRedirects.RecordAsync(_db, "page", p.Slug, model.Slug);
        p.Title = model.Title; p.Slug = model.Slug; p.Content = model.Content;
        p.IsActive = model.IsActive; p.ShowInFooter = model.ShowInFooter; p.SortOrder = model.SortOrder;
        p.SeoTitle = model.SeoTitle; p.SeoDescription = model.SeoDescription;
        await _db.SaveChangesAsync();
        _site.Invalidate();
        TempData["Ok"] = "تغییرات ذخیره شد";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.ContentPages.FindAsync(id);
        if (p is null) return NotFound();
        _db.ContentPages.Remove(p);
        await _db.SaveChangesAsync();
        _site.Invalidate();
        TempData["Ok"] = "صفحه حذف شد";
        return RedirectToAction(nameof(Index));
    }

    private async Task PrepareSlugAsync(ContentPage m)
    {
        var typed = !string.IsNullOrWhiteSpace(m.Slug);
        var slug = SlugHelper.Generate(typed ? m.Slug : m.Title);
        ModelState.Remove(nameof(ContentPage.Slug));
        if (typed)
        {
            if (await _db.ContentPages.AnyAsync(p => p.Slug == slug && p.Id != m.Id))
                ModelState.AddModelError(nameof(ContentPage.Slug), "این آدرس قبلاً استفاده شده است");
        }
        else
        {
            var s = slug; var i = 2;
            while (await _db.ContentPages.AnyAsync(p => p.Slug == s && p.Id != m.Id)) s = $"{slug}-{i++}";
            slug = s;
        }
        m.Slug = slug;
    }
}
