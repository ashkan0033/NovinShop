using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovinShop.Data;
using NovinShop.Models;
using NovinShop.Services;

namespace NovinShop.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = AppRoles.CatalogStaff)]
public class CategoriesController : Controller
{
    private readonly AppDbContext _db;
    private readonly SiteSettings _site;
    public CategoriesController(AppDbContext db, SiteSettings site) { _db = db; _site = site; }

    public async Task<IActionResult> Index() =>
        View(await _db.Categories.AsNoTracking().Include(c => c.Products)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync());

    public IActionResult Create() => View("Form", new Category());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Slug,Description,SeoTitle,SeoDescription,IsActive,SortOrder,Icon")] Category model)
    {
        await PrepareSlugAsync(model);
        if (!ModelState.IsValid) return View("Form", model);
        _db.Categories.Add(model);
        await _db.SaveChangesAsync();
        _site.Invalidate();
        TempData["Ok"] = "دسته‌بندی ثبت شد";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var c = await _db.Categories.FindAsync(id);
        return c is null ? NotFound() : View("Form", c);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Name,Slug,Description,SeoTitle,SeoDescription,IsActive,SortOrder,Icon")] Category model)
    {
        var c = await _db.Categories.FindAsync(id);
        if (c is null) return NotFound();
        model.Id = id;
        await PrepareSlugAsync(model);
        if (!ModelState.IsValid) return View("Form", model);

        await SlugRedirects.RecordAsync(_db, "category", c.Slug, model.Slug);
        c.Name = model.Name; c.Slug = model.Slug; c.Description = model.Description;
        c.SeoTitle = model.SeoTitle; c.SeoDescription = model.SeoDescription;
        c.IsActive = model.IsActive; c.SortOrder = model.SortOrder; c.Icon = model.Icon;
        await _db.SaveChangesAsync();
        _site.Invalidate();
        TempData["Ok"] = "تغییرات ذخیره شد";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Categories.FindAsync(id);
        if (c is null) return NotFound();
        if (await _db.Products.AnyAsync(p => p.CategoryId == id))
        {
            TempData["Err"] = "این دسته‌بندی محصول دارد؛ ابتدا محصولات را منتقل یا حذف کنید";
            return RedirectToAction(nameof(Index));
        }
        _db.Categories.Remove(c);
        await _db.SaveChangesAsync();
        _site.Invalidate();
        TempData["Ok"] = "دسته‌بندی حذف شد";
        return RedirectToAction(nameof(Index));
    }

    // slug خالی → خودکار از نام؛ slug دستی تکراری → خطا
    private async Task PrepareSlugAsync(Category m)
    {
        var typed = !string.IsNullOrWhiteSpace(m.Slug);
        var slug = SlugHelper.Generate(typed ? m.Slug : m.Name);
        ModelState.Remove(nameof(Category.Slug));
        if (typed)
        {
            if (await _db.Categories.AnyAsync(c => c.Slug == slug && c.Id != m.Id))
                ModelState.AddModelError(nameof(Category.Slug), "این آدرس قبلاً استفاده شده است");
        }
        else
        {
            var s = slug; var i = 2;
            while (await _db.Categories.AnyAsync(c => c.Slug == s && c.Id != m.Id)) s = $"{slug}-{i++}";
            slug = s;
        }
        m.Slug = slug;
    }
}
