using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovinShop.Data;
using NovinShop.Models;

namespace NovinShop.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var active = _db.Products.AsNoTracking().Include(p => p.Category).Where(p => p.IsActive);

        // پرفروش‌ها از روی سفارش‌های واقعی (پرداخت‌شده یا ارسال‌شده)
        var soldIds = await _db.OrderItems
            .Where(i => i.Order!.Status == OrderStatus.Paid || i.Order.Status == OrderStatus.Shipped)
            .GroupBy(i => i.ProductId)
            .Select(g => new { Id = g.Key, Qty = g.Sum(x => x.Quantity) })
            .OrderByDescending(x => x.Qty).Take(10).Select(x => x.Id).ToListAsync();
        var best = soldIds.Count == 0
            ? new List<Product>()
            : (await active.Where(p => soldIds.Contains(p.Id)).ToListAsync()).OrderBy(p => soldIds.IndexOf(p.Id)).ToList();

        var categories = await _db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        var allActiveProducts = await active.OrderByDescending(p => p.Id).ToListAsync();

        // ویترین اختصاصی هر دسته‌بندی به ترتیب SortOrder
        var categorySections = categories
            .Select(c =>
            {
                var catProducts = allActiveProducts.Where(p => p.CategoryId == c.Id).ToList();
                return new CategoryShowcase(c, catProducts.Take(10).ToList(), catProducts.Count);
            })
            .Where(cs => cs.Products.Count > 0)
            .ToList();

        // جدیدترین محصولات به صورت متنوع و متوازن از تمام دسته‌بندی‌ها (Round-Robin)
        var latestMixed = new List<Product>();
        var maxDepth = categorySections.Count == 0 ? 0 : categorySections.Max(cs => cs.Products.Count);
        for (int depth = 0; depth < maxDepth && latestMixed.Count < 10; depth++)
        {
            foreach (var cs in categorySections)
            {
                if (depth < cs.Products.Count && latestMixed.Count < 10)
                    latestMixed.Add(cs.Products[depth]);
            }
        }

        var deals = allActiveProducts
            .Where(p => p.OldPrice != null && p.Stock > 0)
            .Take(10)
            .ToList();

        var banners = await _db.Banners.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Id)
            .ToListAsync();

        var blogPosts = await _db.BlogPosts.AsNoTracking()
            .Where(bp => bp.IsPublished)
            .OrderByDescending(bp => bp.Id)
            .Take(8)
            .ToListAsync();

        var vm = new HomeViewModel(categories, latestMixed, deals, banners, best, categorySections, blogPosts);
        return View(vm);
    }

    public IActionResult Error() => StatusCode(500, "خطایی در سرور رخ داد. لطفاً دوباره تلاش کنید.");
}
