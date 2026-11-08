using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovinShop.Data;
using NovinShop.Models;

namespace NovinShop.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = AppRoles.CatalogStaff)]
public class BannersController : Controller
{
    private const string Fields = "Title,Subtitle,ButtonText,LinkUrl,IsActive,SortOrder";
    private static readonly string[] AllowedExt = { ".jpg", ".jpeg", ".png", ".webp" };
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    public BannersController(AppDbContext db, IWebHostEnvironment env) { _db = db; _env = env; }

    public async Task<IActionResult> Index() =>
        View(await _db.Banners.AsNoTracking().OrderBy(b => b.SortOrder).ThenBy(b => b.Id).ToListAsync());

    public IActionResult Create() => View("Form", new Banner());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Fields)] Banner model, IFormFile? image)
    {
        ValidateImage(image);
        if (!ModelState.IsValid) return View("Form", model);
        if (image is { Length: > 0 }) model.ImagePath = await SaveImageAsync(image);
        _db.Banners.Add(model);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "بنر ثبت شد";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var b = await _db.Banners.FindAsync(id);
        return b is null ? NotFound() : View("Form", b);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind(Fields)] Banner model, IFormFile? image)
    {
        var b = await _db.Banners.FindAsync(id);
        if (b is null) return NotFound();
        model.Id = id;
        ValidateImage(image);
        if (!ModelState.IsValid) { model.ImagePath = b.ImagePath; return View("Form", model); }

        b.Title = model.Title; b.Subtitle = model.Subtitle; b.ButtonText = model.ButtonText;
        b.LinkUrl = model.LinkUrl; b.IsActive = model.IsActive; b.SortOrder = model.SortOrder;
        if (image is { Length: > 0 })
        {
            DeleteImage(b.ImagePath);
            b.ImagePath = await SaveImageAsync(image);
        }
        await _db.SaveChangesAsync();
        TempData["Ok"] = "تغییرات ذخیره شد";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var b = await _db.Banners.FindAsync(id);
        if (b is null) return NotFound();
        DeleteImage(b.ImagePath);
        _db.Banners.Remove(b);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "بنر حذف شد";
        return RedirectToAction(nameof(Index));
    }

    private void ValidateImage(IFormFile? f)
    {
        if (f is null || f.Length == 0) return;
        if (!AllowedExt.Contains(Path.GetExtension(f.FileName).ToLowerInvariant()))
            ModelState.AddModelError("image", "فقط تصاویر JPG، PNG و WebP مجاز است");
        else if (f.Length > 3 * 1024 * 1024)
            ModelState.AddModelError("image", "حجم تصویر نباید بیش از ۳ مگابایت باشد");
    }

    private string Dir => Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "banners");

    private async Task<string> SaveImageAsync(IFormFile file)
    {
        Directory.CreateDirectory(Dir);
        var name = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName).ToLowerInvariant();
        await using var fs = System.IO.File.Create(Path.Combine(Dir, name));
        await file.CopyToAsync(fs);
        return "/uploads/banners/" + name;
    }

    private void DeleteImage(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        var full = Path.Combine(_env.ContentRootPath, "wwwroot", path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
    }
}
