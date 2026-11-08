using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovinShop.Data;
using NovinShop.Models;
using NovinShop.Services;

namespace NovinShop.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = AppRoles.CatalogStaff)]
public class BlogController : Controller
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public BlogController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<IActionResult> Index(string? q = null)
    {
        var query = _db.BlogPosts.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Title.Contains(q) || p.Category.Contains(q));

        ViewBag.Q = q;
        var items = await query.OrderByDescending(p => p.Id).ToListAsync();
        return View(items);
    }

    public IActionResult Create()
    {
        ViewBag.TinyMceApiKey = _config["TinyMCE:ApiKey"];
        return View("Form", new BlogPost());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BlogPost model)
    {
        await PrepareAndValidateAsync(model);
        if (!ModelState.IsValid)
        {
            ViewBag.TinyMceApiKey = _config["TinyMCE:ApiKey"];
            return View("Form", model);
        }

        model.Content = HtmlSanitizerService.Sanitize(model.Content);
        model.CreatedAt = Fmt.TehranNow();
        _db.BlogPosts.Add(model);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "مقاله جدید با موفقیت منتشر شد.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.BlogPosts.FindAsync(id);
        if (item is null) return NotFound();
        ViewBag.TinyMceApiKey = _config["TinyMCE:ApiKey"];
        return View("Form", item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BlogPost model)
    {
        var existing = await _db.BlogPosts.FindAsync(id);
        if (existing is null) return NotFound();

        model.Id = id;
        await PrepareAndValidateAsync(model);
        if (!ModelState.IsValid)
        {
            ViewBag.TinyMceApiKey = _config["TinyMCE:ApiKey"];
            return View("Form", model);
        }

        existing.Title = model.Title;
        existing.Slug = model.Slug;
        existing.Category = model.Category;
        existing.Summary = model.Summary;
        existing.Content = HtmlSanitizerService.Sanitize(model.Content);
        existing.Icon = string.IsNullOrWhiteSpace(model.Icon) ? "📰" : model.Icon.Trim();
        existing.Gradient = string.IsNullOrWhiteSpace(model.Gradient) ? "linear-gradient(135deg,#1e3a8a,#0f172a)" : model.Gradient.Trim();
        existing.ReadingMinutes = Math.Clamp(model.ReadingMinutes, 1, 120);
        existing.IsPublished = model.IsPublished;

        await _db.SaveChangesAsync();
        TempData["Ok"] = "تغییرات مقاله ذخیره شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.BlogPosts.FindAsync(id);
        if (item is not null)
        {
            _db.BlogPosts.Remove(item);
            await _db.SaveChangesAsync();
            TempData["Ok"] = "مقاله حذف شد.";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task PrepareAndValidateAsync(BlogPost model)
    {
        model.Slug = SlugHelper.Generate(string.IsNullOrWhiteSpace(model.Slug) ? model.Title : model.Slug);
        ModelState.Remove(nameof(BlogPost.Slug));
        if (string.IsNullOrEmpty(model.Slug))
            ModelState.AddModelError(nameof(BlogPost.Slug), "نامک (Slug) معتبر نیست.");
        else if (await _db.BlogPosts.AnyAsync(p => p.Slug == model.Slug && p.Id != model.Id))
            ModelState.AddModelError(nameof(BlogPost.Slug), "این نامک قبلاً برای مقاله دیگری ثبت شده است.");
    }
}
