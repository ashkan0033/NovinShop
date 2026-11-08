using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovinShop.Data;
using NovinShop.Models;

namespace NovinShop.Areas.Admin.Controllers;

public record OrderProfitRow(
    Order Order,
    long TotalCostOfGoods,
    long GrossProfit);

public record AccountingDashboardViewModel(
    long TotalGrossSales,          // جمع فروش کالاها قبل از تخفیف
    long TotalDiscountsGiven,      // جمع تخفیف‌های کوپن
    long TotalShippingCollected,   // جمع هزینه ارسال دریافتی
    long TotalNetRevenue,          // کل مبلغ دریافتی از سفارش‌های پرداخت‌شده/ارسال‌شده
    long TotalCostOfGoodsSold,     // بهای تمام‌شده کالای فروش‌رفته (COGS)
    long GrossSalesProfit,         // سود ناخالص حاصل از فروش کالاها
    long TotalInventoryPurchased,  // کل بهای خرید کالاهای واردشده به انبار
    long CurrentWarehouseCostVal,  // ارزش فعلی انبار به قیمت خرید
    long CurrentWarehouseSaleVal,  // ارزش فعلی انبار به قیمت فروش
    long ExtraIncome,              // سایر درآمدهای ثبت‌شده
    long OperatingExpenses,        // هزینه‌های عملیاتی و جانبی (به‌جز خرید کالا)
    long NetFinalProfit,           // سود خالص نهایی فروشگاه
    List<OrderProfitRow> RecentPaidOrders,
    List<AccountingEntry> Entries);

[Area("Admin"), Authorize(Roles = AppRoles.OrderAndFinanceStaff)]
public class AccountingController : Controller
{
    private readonly AppDbContext _db;
    public AccountingController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var paidOrders = await _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.Status == OrderStatus.Paid || o.Status == OrderStatus.Shipped)
            .OrderByDescending(o => o.Id)
            .ToListAsync();

        var products = await _db.Products.AsNoTracking().ToDictionaryAsync(p => p.Id);

        long totalGrossSales = 0;
        long totalDiscounts = 0;
        long totalShipping = 0;
        long totalNetRevenue = 0;
        long totalCogs = 0;
        var orderRows = new List<OrderProfitRow>();

        foreach (var o in paidOrders)
        {
            totalGrossSales += o.Subtotal;
            totalDiscounts += o.DiscountAmount;
            totalShipping += o.Shipping;
            totalNetRevenue += o.Total;

            long orderCogs = 0;
            foreach (var item in o.Items)
            {
                var unitCost = item.UnitCost;
                if (unitCost <= 0 && products.TryGetValue(item.ProductId, out var prod))
                {
                    unitCost = prod.CostPrice > 0 ? prod.CostPrice : (int)Math.Round(item.UnitPrice * 0.72);
                }
                else if (unitCost <= 0)
                {
                    unitCost = (int)Math.Round(item.UnitPrice * 0.72);
                }
                orderCogs += (long)unitCost * item.Quantity;
            }
            totalCogs += orderCogs;
            var netOrderGoods = Math.Max(0, o.Subtotal - o.DiscountAmount);
            orderRows.Add(new OrderProfitRow(o, orderCogs, netOrderGoods - orderCogs));
        }

        var grossProfit = Math.Max(0, totalGrossSales - totalDiscounts) - totalCogs;

        var stockInTxs = await _db.InventoryTransactions.AsNoTracking()
            .Where(t => t.Type == InventoryTxType.StockIn && t.QuantityChange > 0)
            .ToListAsync();
        var totalInventoryPurchased = stockInTxs.Sum(t => (long)t.QuantityChange * t.UnitCost);

        var currentCostVal = products.Values.Sum(p => (long)p.Stock * (p.CostPrice > 0 ? p.CostPrice : (int)Math.Round(p.Price * 0.72)));
        var currentSaleVal = products.Values.Sum(p => (long)p.Stock * p.Price);

        var entries = await _db.AccountingEntries.AsNoTracking()
            .OrderByDescending(e => e.Id)
            .Take(80)
            .ToListAsync();

        var extraIncome = entries.Where(e => e.Type == AccountingEntryType.Income).Sum(e => e.Amount);
        // هزینه‌های عملیاتی غیر از خرید مستقیم کالا (چون بهای کالای فروش‌رفته در COGS لحاظ شده است)
        var operatingExpenses = entries
            .Where(e => e.Type == AccountingEntryType.Expense && !e.Category.Contains("ورودی انبار"))
            .Sum(e => e.Amount);

        var netFinalProfit = grossProfit + extraIncome - operatingExpenses;

        return View(new AccountingDashboardViewModel(
            totalGrossSales,
            totalDiscounts,
            totalShipping,
            totalNetRevenue,
            totalCogs,
            grossProfit,
            totalInventoryPurchased,
            currentCostVal,
            currentSaleVal,
            extraIncome,
            operatingExpenses,
            netFinalProfit,
            orderRows.Take(15).ToList(),
            entries));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddEntry(AccountingEntryType type, string category, string title, long amount, string? reference, string? note)
    {
        if (string.IsNullOrWhiteSpace(title) || amount <= 0)
        {
            TempData["Err"] = "لطفاً شرح سند مالی و مبلغ معتبر وارد کنید.";
            return RedirectToAction(nameof(Index));
        }

        _db.AccountingEntries.Add(new AccountingEntry
        {
            Type = type,
            Category = string.IsNullOrWhiteSpace(category) ? "سایر" : category.Trim(),
            Title = title.Trim()[..Math.Min(title.Trim().Length, 200)],
            Amount = amount,
            Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim(),
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedBy = User.Identity?.Name ?? "Admin",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        TempData["Ok"] = $"سند مالی «{title}» به مبلغ {NovinShop.Services.Fmt.Toman(amount)} ثبت شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEntry(int id)
    {
        var e = await _db.AccountingEntries.FindAsync(id);
        if (e is not null)
        {
            _db.AccountingEntries.Remove(e);
            await _db.SaveChangesAsync();
            TempData["Ok"] = "سند مالی حذف شد.";
        }
        return RedirectToAction(nameof(Index));
    }
}
