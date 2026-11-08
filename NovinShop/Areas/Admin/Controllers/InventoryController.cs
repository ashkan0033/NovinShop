using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovinShop.Data;
using NovinShop.Models;
using NovinShop.Services;

namespace NovinShop.Areas.Admin.Controllers;

public record InventoryViewModel(
    List<Product> Products,
    List<InventoryTransaction> Transactions,
    int? SelectedProductId,
    InventoryTxType? SelectedType,
    int TotalUnits,
    long TotalCostValue,
    long TotalSaleValue,
    int LowStockCount);

[Area("Admin"), Authorize(Roles = AppRoles.WarehouseStaff)]
public class InventoryController : Controller
{
    private readonly AppDbContext _db;
    public InventoryController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? productId, InventoryTxType? type)
    {
        var products = await _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .OrderBy(p => p.Stock)
            .ThenBy(p => p.Name)
            .ToListAsync();

        var txQuery = _db.InventoryTransactions.AsNoTracking()
            .Include(t => t.Product)
            .AsQueryable();

        if (productId is > 0)
            txQuery = txQuery.Where(t => t.ProductId == productId);
        if (type is not null)
            txQuery = txQuery.Where(t => t.Type == type);

        var txs = await txQuery.OrderByDescending(t => t.Id).Take(60).ToListAsync();

        var totalUnits = products.Sum(p => p.Stock);
        var totalCostVal = products.Sum(p => (long)p.Stock * (p.CostPrice > 0 ? p.CostPrice : (int)Math.Round(p.Price * 0.72)));
        var totalSaleVal = products.Sum(p => (long)p.Stock * p.Price);
        var lowStock = products.Count(p => p.Stock <= 3);

        return View(new InventoryViewModel(
            products, txs, productId, type, totalUnits, totalCostVal, totalSaleVal, lowStock));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordMovement(
        int productId,
        InventoryTxType type,
        int qty,
        int unitCost,
        string? reference,
        string? note,
        bool recordInAccounting = true)
    {
        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == productId);
        if (p is null)
        {
            TempData["Err"] = "لطفاً کالای مورد نظر را انتخاب کنید.";
            return RedirectToAction(nameof(Index));
        }

        if (qty == 0)
        {
            TempData["Err"] = "تعداد کالا نمی‌تواند صفر باشد.";
            return RedirectToAction(nameof(Index), new { productId });
        }

        int change = type switch
        {
            InventoryTxType.StockIn or InventoryTxType.ReturnIn => Math.Abs(qty),
            InventoryTxType.WasteOut or InventoryTxType.SaleOut => -Math.Abs(qty),
            _ => qty // در حالت Adjustment می‌تواند مثبت یا منفی باشد
        };

        if (p.Stock + change < 0)
        {
            TempData["Err"] = $"موجودی فعلی «{p.Name}» ({Fmt.Num(p.Stock)} عدد) کمتر از میزان کسر درخواستی است.";
            return RedirectToAction(nameof(Index), new { productId });
        }

        var finalUnitCost = unitCost > 0 ? unitCost : (p.CostPrice > 0 ? p.CostPrice : (int)Math.Round(p.Price * 0.72));

        await using var tx = await _db.Database.BeginTransactionAsync();

        p.Stock += change;
        if (type == InventoryTxType.StockIn && unitCost > 0)
        {
            p.CostPrice = unitCost;
        }

        var invTx = new InventoryTransaction
        {
            ProductId = p.Id,
            Type = type,
            QuantityChange = change,
            StockAfter = p.Stock,
            UnitCost = finalUnitCost,
            Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim()[..Math.Min(reference.Trim().Length, 120)],
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()[..Math.Min(note.Trim().Length, 300)],
            CreatedBy = User.Identity?.Name ?? "Admin",
            CreatedAt = DateTime.UtcNow
        };
        _db.InventoryTransactions.Add(invTx);

        // اگر ورود کالا به انبار (خرید) باشد و تیک ثبت خودکار در حسابداری فعال باشد
        if (type == InventoryTxType.StockIn && recordInAccounting && change > 0 && finalUnitCost > 0)
        {
            _db.AccountingEntries.Add(new AccountingEntry
            {
                Type = AccountingEntryType.Expense,
                Category = "خرید و تأمین کالا (ورودی انبار)",
                Title = $"خرید {Fmt.Num(change)} عدد {p.Name} به قیمت واحد {Fmt.Toman(finalUnitCost)}",
                Amount = (long)change * finalUnitCost,
                Reference = invTx.Reference,
                Note = invTx.Note,
                CreatedBy = invTx.CreatedBy,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        TempData["Ok"] = $"حواله «{type.Label()}» برای «{p.Name}» ثبت شد. موجودی جدید در انبار: {Fmt.Num(p.Stock)} عدد.";
        return RedirectToAction(nameof(Index), new { productId = p.Id });
    }
}
