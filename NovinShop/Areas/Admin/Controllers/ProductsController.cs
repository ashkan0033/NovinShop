using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NovinShop.Data;
using NovinShop.Models;
using NovinShop.Services;

namespace NovinShop.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = AppRoles.CatalogStaff)]
public class ProductsController : Controller
{
    private const int PageSize = 10;
    private const string Fields = "Name,Slug,ShortDescription,Description,Price,OldPrice,IsActive,CategoryId,SeoTitle,SeoDescription";
    private static readonly string[] AllowedExt = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    public ProductsController(AppDbContext db, IWebHostEnvironment env) { _db = db; _env = env; }

    // آپلود تصویر از داخل ویرایشگر TinyMCE 8 (فقط ادمین + توکن Anti-Forgery)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadEditorImage(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "فایلی برای آپلود انتخاب نشده است" });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExt.Contains(ext))
            return BadRequest(new { error = "فقط تصاویر JPG، PNG، WebP و GIF مجاز است" });

        if (file.Length > 2 * 1024 * 1024)
            return BadRequest(new { error = "حجم تصویر نباید بیش از ۲ مگابایت باشد" });

        if (!await IsValidImageSignatureAsync(file))
            return BadRequest(new { error = "محتوای فایل تصویر معتبر نیست" });

        var dir = Path.Combine(UploadsDir, "editor");
        Directory.CreateDirectory(dir);
        var name = Guid.NewGuid().ToString("N") + ext;
        await using var fs = System.IO.File.Create(Path.Combine(dir, name));
        await file.CopyToAsync(fs);

        return Json(new { location = "/uploads/products/editor/" + name });
    }

    private static async Task<bool> IsValidImageSignatureAsync(IFormFile file)
    {
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
        if (read < 4) return false;
        // JPEG: FF D8 FF
        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return true;
        // PNG: 89 50 4E 47
        if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47) return true;
        // GIF: 47 49 46 38
        if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38) return true;
        // WebP: RIFF....WEBP
        if (read >= 12 && header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F' &&
            header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P') return true;
        return false;
    }

    public async Task<IActionResult> Index(string? q, int? categoryId, int page = 1)
    {
        var query = _db.Products.AsNoTracking().Include(p => p.Category).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(p => p.Name.Contains(q.Trim()));
        if (categoryId is not null) query = query.Where(p => p.CategoryId == categoryId);

        var total = await query.CountAsync();
        var pages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, pages);
        var items = await query.OrderByDescending(p => p.Id).Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

        ViewBag.Categories = await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        return View(new ProductListViewModel(items, q, categoryId, page, pages, total));
    }

    public async Task<IActionResult> Create()
    {
        await LoadCategoriesAsync();
        return View("Form", new Product());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Fields)] Product model, IFormFile? image, List<IFormFile>? galleryFiles, string[]? specKeys, string[]? specValues)
    {
        model.SpecsJson = BuildSpecsJson(specKeys, specValues);
        await ValidateAsync(model, image, galleryFiles);
        if (!ModelState.IsValid) { await LoadCategoriesAsync(model.CategoryId); return View("Form", model); }

        if (image is { Length: > 0 }) model.ImagePath = await SaveImageAsync(image);
        if (galleryFiles is { Count: > 0 })
        {
            var gList = new List<string>();
            foreach (var gf in galleryFiles.Where(f => f.Length > 0))
                gList.Add(await SaveImageAsync(gf));
            model.GalleryJson = gList.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(gList) : null;
        }
        model.Description = HtmlSanitizerService.Sanitize(model.Description);
        model.Stock = 0; // موجودی فقط از طریق ماژول انبارداری وارد می‌شود
        model.CreatedAt = DateTime.UtcNow;
        _db.Products.Add(model);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "محصول ثبت شد. اکنون می‌توانید از بخش «انبارداری» حواله ورود موجودی و قیمت خرید آن را ثبت کنید.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p is null) return NotFound();
        await LoadCategoriesAsync(p.CategoryId);
        return View("Form", p);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind(Fields)] Product model, IFormFile? image, List<IFormFile>? galleryFiles, string[]? keepGallery, string[]? specKeys, string[]? specValues)
    {
        var p = await _db.Products.FindAsync(id);
        if (p is null) return NotFound();
        model.Id = id;
        model.SpecsJson = BuildSpecsJson(specKeys, specValues);
        await ValidateAsync(model, image, galleryFiles);
        if (!ModelState.IsValid)
        {
            model.ImagePath = p.ImagePath;
            model.GalleryJson = p.GalleryJson;
            model.Stock = p.Stock;
            model.CostPrice = p.CostPrice;
            await LoadCategoriesAsync(model.CategoryId);
            return View("Form", model);
        }

        await SlugRedirects.RecordAsync(_db, "product", p.Slug, model.Slug);
        p.Name = model.Name; p.Slug = model.Slug; p.ShortDescription = model.ShortDescription;
        p.Description = HtmlSanitizerService.Sanitize(model.Description); p.Price = model.Price; p.OldPrice = model.OldPrice;
        p.IsActive = model.IsActive; p.CategoryId = model.CategoryId;
        p.SeoTitle = model.SeoTitle; p.SeoDescription = model.SeoDescription;
        p.SpecsJson = model.SpecsJson;

        if (image is { Length: > 0 })
        {
            DeleteImage(p.ImagePath);
            p.ImagePath = await SaveImageAsync(image);
        }

        var oldGallery = p.GetGallery();
        var kept = keepGallery?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();
        foreach (var removedImg in oldGallery.Where(x => !kept.Contains(x)))
            DeleteImage(removedImg);

        var finalGallery = oldGallery.Where(x => kept.Contains(x)).ToList();
        if (galleryFiles is { Count: > 0 })
        {
            foreach (var gf in galleryFiles.Where(f => f.Length > 0))
                finalGallery.Add(await SaveImageAsync(gf));
        }
        p.GalleryJson = finalGallery.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(finalGallery) : null;

        await _db.SaveChangesAsync();
        TempData["Ok"] = "تغییرات ذخیره شد";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p is null) return NotFound();
        DeleteImage(p.ImagePath);
        foreach (var g in p.GetGallery()) DeleteImage(g);
        _db.Products.Remove(p);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "محصول حذف شد";
        return RedirectToAction(nameof(Index));
    }

    // ───────── کمکی‌ها ─────────
    private async Task LoadCategoriesAsync(int? selected = null) =>
        ViewBag.CategoryList = new SelectList(
            await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(), "Id", "Name", selected);

    private static string? BuildSpecsJson(string[]? keys, string[]? values)
    {
        if (keys is null || values is null) return null;
        var list = new List<ProductSpec>();
        var len = Math.Min(keys.Length, values.Length);
        for (int i = 0; i < len; i++)
        {
            var k = (keys[i] ?? "").Trim();
            var v = (values[i] ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(k) && !string.IsNullOrWhiteSpace(v))
                list.Add(new ProductSpec(k, v));
        }
        return list.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(list) : null;
    }

    private async Task ValidateAsync(Product m, IFormFile? image, List<IFormFile>? galleryFiles = null)
    {
        // slug
        var typed = !string.IsNullOrWhiteSpace(m.Slug);
        var slug = SlugHelper.Generate(typed ? m.Slug : m.Name);
        ModelState.Remove(nameof(Product.Slug));
        if (typed)
        {
            if (await _db.Products.AnyAsync(p => p.Slug == slug && p.Id != m.Id))
                ModelState.AddModelError(nameof(Product.Slug), "این آدرس قبلاً استفاده شده است");
        }
        else
        {
            var s = slug; var i = 2;
            while (await _db.Products.AnyAsync(p => p.Slug == s && p.Id != m.Id)) s = $"{slug}-{i++}";
            slug = s;
        }
        m.Slug = slug;

        // دسته‌بندی و قیمت
        if (m.CategoryId > 0 && !await _db.Categories.AnyAsync(c => c.Id == m.CategoryId))
            ModelState.AddModelError(nameof(Product.CategoryId), "دسته‌بندی معتبر نیست");
        if (m.OldPrice is { } old && old <= m.Price)
            ModelState.AddModelError(nameof(Product.OldPrice), "قیمت قبل از تخفیف باید بیشتر از قیمت فعلی باشد");

        // تصویر اصلی
        if (image is { Length: > 0 })
        {
            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!AllowedExt.Contains(ext)) ModelState.AddModelError("image", "فقط تصاویر JPG، PNG و WebP مجاز است");
            else if (image.Length > 2 * 1024 * 1024) ModelState.AddModelError("image", "حجم تصویر نباید بیش از ۲ مگابایت باشد");
        }

        // تصاویر گالری
        if (galleryFiles is { Count: > 0 })
        {
            foreach (var gf in galleryFiles.Where(f => f.Length > 0))
            {
                var ext = Path.GetExtension(gf.FileName).ToLowerInvariant();
                if (!AllowedExt.Contains(ext)) { ModelState.AddModelError("galleryFiles", "تمامی تصاویر گالری باید JPG، PNG یا WebP باشند"); break; }
                if (gf.Length > 2 * 1024 * 1024) { ModelState.AddModelError("galleryFiles", "حجم هر تصویر گالری نباید بیش از ۲ مگابایت باشد"); break; }
            }
        }
    }

    private string UploadsDir => Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "products");

    private async Task<string> SaveImageAsync(IFormFile file)
    {
        Directory.CreateDirectory(UploadsDir);
        // نام فایل کاربر استفاده نمی‌شود (امنیت)؛ فقط پسوند مجاز
        var name = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName).ToLowerInvariant();
        await using var fs = System.IO.File.Create(Path.Combine(UploadsDir, name));
        await file.CopyToAsync(fs);
        return "/uploads/products/" + name;
    }

    private void DeleteImage(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        var full = Path.Combine(_env.ContentRootPath, "wwwroot", path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
    }
}
