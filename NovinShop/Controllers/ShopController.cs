using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovinShop.Data;
using NovinShop.Models;
using NovinShop.Services;

namespace NovinShop.Controllers;

public class ShopController : Controller
{
    private const int PageSize = 12;
    private readonly AppDbContext _db;
    public ShopController(AppDbContext db) => _db = db;

    // /products  و  /category/دیجیتال
    [HttpGet("products")]
    [HttpGet("category/{slug}")]
    public async Task<IActionResult> Index(string? slug, string? q, int? min, int? max, bool inStock = false, bool sale = false, string sort = "new", int page = 1)
    {
        Category? cat = null;
        var query = _db.Products.AsNoTracking().Include(p => p.Category).Where(p => p.IsActive);

        if (slug is not null)
        {
            cat = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive);
            if (cat is null)
            {
                var r = await SlugRedirects.FindAsync(_db, "category", slug);
                return r is null ? NotFound() : RedirectToActionPermanent(nameof(Index), new { slug = r.NewSlug });
            }
            query = query.Where(p => p.CategoryId == cat.Id);
        }
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(p => p.Name.Contains(q.Trim()));
        if (min is > 0) query = query.Where(p => p.Price >= min.Value);
        if (max is > 0) query = query.Where(p => p.Price <= max.Value);
        if (inStock) query = query.Where(p => p.Stock > 0);
        if (sale) query = query.Where(p => p.OldPrice != null);

        query = sort switch
        {
            "price-asc" => query.OrderBy(p => p.Price),
            "price-desc" => query.OrderByDescending(p => p.Price),
            _ => query.OrderByDescending(p => p.Id)
        };

        var total = await query.CountAsync();
        var pages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, pages);
        var items = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

        var heading = cat?.Name ?? (sale ? "پیشنهادهای ویژه" : string.IsNullOrWhiteSpace(q) ? "همه‌ی محصولات" : $"نتایج جستجو برای «{q.Trim()}»");
        ViewData["Title"] = cat is null ? $"{heading} | نوین‌شاپ" : cat.SeoTitle ?? $"خرید {cat.Name} | نوین‌شاپ";
        ViewData["Description"] = cat?.SeoDescription ?? cat?.Description ?? "مشاهده و خرید آنلاین محصولات با ضمانت اصالت و ارسال سریع";
        if (page > 1) ViewData["Canonical"] = $"{Request.Scheme}://{Request.Host}{Request.Path.ToUriComponent()}?page={page}";
        if (sort != "new" || min != null || max != null || inStock) ViewData["Robots"] = "noindex,follow";          // نسخه‌های مرتب‌شده ایندکس نشوند
        if (!string.IsNullOrWhiteSpace(q)) ViewData["Robots"] = "noindex,follow";   // صفحه‌ی جستجو ایندکس نشود

        ViewBag.Categories = await _db.Categories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();
        return View(new ShopListViewModel(heading, cat, items, q, sort, page, pages, total));
    }

    // /product/هدفون-بی-سیم
    [HttpGet("product/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var p = await _db.Products.AsNoTracking().Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive);
        if (p is null)
        {
            var r = await SlugRedirects.FindAsync(_db, "product", slug);
            return r is null ? NotFound() : RedirectToActionPermanent(nameof(Details), new { slug = r.NewSlug });
        }

        var related = await _db.Products.AsNoTracking().Include(x => x.Category)
            .Where(x => x.IsActive && x.CategoryId == p.CategoryId && x.Id != p.Id)
            .OrderByDescending(x => x.Id).Take(4).ToListAsync();

        var reviews = await _db.ProductReviews.AsNoTracking()
            .Where(r => r.ProductId == p.Id && r.IsApproved)
            .OrderByDescending(r => r.Id)
            .ToListAsync();

        ViewData["Title"] = p.SeoTitle ?? $"خرید {p.Name} | نوین‌شاپ";
        ViewData["Description"] = p.SeoDescription ?? p.ShortDescription ?? p.Name;
        ViewData["OgImage"] = p.ImagePath;
        return View(new ProductPageViewModel(p, related, reviews));
    }

    [HttpPost("product/{slug}/review"), ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReview(string slug, string? authorName, int rating = 5, string? title = null, string? comment = null, string? pros = null, string? cons = null)
    {
        var p = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive);
        if (p is null) return NotFound();

        var name = (authorName ?? "").Trim();
        var text = (comment ?? "").Trim();
        if (name.Length < 2 || text.Length < 5)
        {
            TempData["Err"] = "لطفاً نام خود و متن دیدگاه (حداقل ۵ کاراکتر) را وارد کنید.";
            return RedirectToAction(nameof(Details), new { slug });
        }

        // بررسی اینکه آیا کاربر در سفارش‌های قبلی خود این محصول را خریده است
        var isBuyer = false;
        var sessionOrdersJson = HttpContext.Session.GetString("orders");
        if (!string.IsNullOrEmpty(sessionOrdersJson))
        {
            var myCodes = System.Text.Json.JsonSerializer.Deserialize<HashSet<string>>(sessionOrdersJson) ?? new();
            if (myCodes.Count > 0)
            {
                isBuyer = await _db.OrderItems.AnyAsync(i =>
                    myCodes.Contains(i.Order!.Code) &&
                    (i.Order.Status == OrderStatus.Paid || i.Order.Status == OrderStatus.Shipped) &&
                    i.ProductId == p.Id);
            }
        }

        var rev = new ProductReview
        {
            ProductId = p.Id,
            AuthorName = name[..Math.Min(name.Length, 80)],
            Rating = Math.Clamp(rating, 1, 5),
            Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim()[..Math.Min(title.Trim().Length, 120)],
            Comment = text[..Math.Min(text.Length, 1500)],
            Pros = string.IsNullOrWhiteSpace(pros) ? null : pros.Trim()[..Math.Min(pros.Trim().Length, 250)],
            Cons = string.IsNullOrWhiteSpace(cons) ? null : cons.Trim()[..Math.Min(cons.Trim().Length, 250)],
            IsBuyer = isBuyer,
            IsApproved = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.ProductReviews.Add(rev);
        await _db.SaveChangesAsync();

        TempData["Ok"] = "دیدگاه و امتیاز شما با موفقیت ثبت شد و پس از بررسی و تأیید مدیریت در صفحه محصول نمایش داده خواهد شد.";
        return RedirectToAction(nameof(Details), new { slug });
    }
}
