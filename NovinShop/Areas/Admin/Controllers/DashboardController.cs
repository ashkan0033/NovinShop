using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovinShop.Data;
using NovinShop.Models;

namespace NovinShop.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = AppRoles.AllStaff)]
public class DashboardController : Controller
{
    private readonly AppDbContext _db;
    public DashboardController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewBag.PendingOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatus.PendingPayment);
        ViewBag.PendingReceipts = await _db.Orders.CountAsync(o => o.Status == OrderStatus.PendingApproval);
        ViewBag.PaidOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Paid);
        ViewBag.Revenue = await _db.Orders
            .Where(o => o.Status == OrderStatus.Paid || o.Status == OrderStatus.Shipped)
            .SumAsync(o => (long?)o.Total) ?? 0L;
        ViewBag.LatestOrders = await _db.Orders.AsNoTracking().OrderByDescending(o => o.Id).Take(6).ToListAsync();
        ViewBag.LowStockList = await _db.Products.AsNoTracking().Where(p => p.IsActive && p.Stock <= 3)
            .OrderBy(p => p.Stock).Take(6).Select(p => new SeoItem(p.Id, p.Name + " — " + p.Stock)).ToListAsync();

        // فروش ۷ روز اخیر (به وقت تهران)
        var since = DateTime.UtcNow.AddDays(-8);
        var recent = await _db.Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= since && (o.Status == OrderStatus.Paid || o.Status == OrderStatus.Shipped))
            .Select(o => new { o.CreatedAt, o.Total }).ToListAsync();
        var today = NovinShop.Services.Fmt.TehranNow().Date;
        var offset = NovinShop.Services.Fmt.TehranNow() - DateTime.UtcNow;
        var pc = new System.Globalization.PersianCalendar();
        ViewBag.Chart = Enumerable.Range(0, 7).Select(i => today.AddDays(i - 6)).Select(day => (
            Label: NovinShop.Services.Fmt.Fa($"{pc.GetMonth(day):00}/{pc.GetDayOfMonth(day):00}"),
            Sum: recent.Where(o => (o.CreatedAt + offset).Date == day).Sum(o => o.Total))).ToList();

        return View(new DashboardViewModel(
            await _db.Products.CountAsync(),
            await _db.Products.CountAsync(p => p.IsActive),
            await _db.Categories.CountAsync(),
            await _db.Products.CountAsync(p => p.IsActive && p.Stock <= 3),
            await _db.Products.AsNoTracking().Include(p => p.Category).OrderByDescending(p => p.Id).Take(5).ToListAsync()));
    }
}
