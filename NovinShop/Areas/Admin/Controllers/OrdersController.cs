using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovinShop.Data;
using NovinShop.Models;

namespace NovinShop.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = AppRoles.OrderAndFinanceStaff)]
public class OrdersController : Controller
{
    private const int PageSize = 15;
    private readonly AppDbContext _db;
    public OrdersController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(OrderStatus? status, string? q, int page = 1)
    {
        var query = _db.Orders.AsNoTracking().AsQueryable();
        if (status is not null) query = query.Where(o => o.Status == status);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var t = q.Trim();
            query = query.Where(o => o.Code.Contains(t) || o.Phone.Contains(t) || o.CustomerName.Contains(t));
        }

        var total = await query.CountAsync();
        var pages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, pages);
        var items = await query.OrderByDescending(o => o.Id).Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();
        return View(new OrderListViewModel(items, status, q, page, pages, total));
    }

    public async Task<IActionResult> Details(int id)
    {
        var o = await _db.Orders.AsNoTracking().Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id);
        return o is null ? NotFound() : View(o);
    }

    public async Task<IActionResult> Invoice(int id, string mode = "both")
    {
        var o = await _db.Orders.AsNoTracking().Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id);
        if (o is null) return NotFound();
        ViewBag.IsAdmin = true;
        ViewBag.Mode = mode;
        return View("~/Views/Order/Invoice.cshtml", o);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, OrderStatus status, string? adminNote = null)
    {
        var o = await _db.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id);
        if (o is null) return NotFound();

        var allowed = (o.Status, status) switch
        {
            (OrderStatus.PendingPayment, OrderStatus.Paid) or (OrderStatus.PendingPayment, OrderStatus.Cancelled)
                or (OrderStatus.PendingApproval, OrderStatus.Paid) or (OrderStatus.PendingApproval, OrderStatus.PendingPayment) or (OrderStatus.PendingApproval, OrderStatus.Cancelled)
                or (OrderStatus.Paid, OrderStatus.Shipped) or (OrderStatus.Paid, OrderStatus.Cancelled) => true,
            _ => false
        };
        if (!allowed)
        {
            TempData["Err"] = "این تغییر وضعیت مجاز نیست";
            return RedirectToAction(nameof(Details), new { id });
        }

        await using var tx = await _db.Database.BeginTransactionAsync();
        if (status == OrderStatus.Cancelled)
        {
            foreach (var item in o.Items)   // برگرداندن موجودی به انبار + ثبت در کاردکس انبارداری
            {
                var pid = item.ProductId; var qty = item.Quantity;
                await _db.Products.Where(p => p.Id == pid)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + qty));

                var prodAfter = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == pid);
                if (prodAfter is not null)
                {
                    _db.InventoryTransactions.Add(new InventoryTransaction
                    {
                        ProductId = pid,
                        Type = InventoryTxType.ReturnIn,
                        QuantityChange = qty,
                        StockAfter = prodAfter.Stock,
                        UnitCost = item.UnitCost > 0 ? item.UnitCost : prodAfter.CostPrice,
                        Reference = o.Code,
                        Note = $"برگشت به انبار بابت لغو سفارش {o.Code}",
                        CreatedBy = User.Identity?.Name ?? "Admin",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            if (!string.IsNullOrEmpty(o.CouponCode))
            {
                var cCode = o.CouponCode;
                await _db.Coupons.Where(c => c.Code == cCode && c.UsedCount > 0)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsedCount, c => c.UsedCount - 1));
            }
        }
        if (status == OrderStatus.Paid)
        {
            o.PaidAt = DateTime.UtcNow;
            o.PaymentRef ??= o.PaymentMethod == "card_to_card" ? "C2C-APPROVED" : "MANUAL";
            o.AdminNote = null;
        }
        if (status == OrderStatus.PendingPayment && !string.IsNullOrWhiteSpace(adminNote))
        {
            o.AdminNote = adminNote.Trim();
        }
        o.Status = status;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        TempData["Ok"] = $"وضعیت سفارش به «{status.Label()}» تغییر کرد";
        return RedirectToAction(nameof(Details), new { id });
    }
}
