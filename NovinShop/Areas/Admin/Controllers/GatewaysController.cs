using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovinShop.Data;
using NovinShop.Models;

namespace NovinShop.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "Admin")]
public class GatewaysController : Controller
{
    private readonly AppDbContext _db;
    public GatewaysController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var list = await _db.PaymentGateways.AsNoTracking().OrderBy(g => g.SortOrder).ToListAsync();
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        int id, string title, string? description, bool isEnabled, bool isSandbox, int sortOrder,
        string? merchantId, string? cardNumber, string? cardHolder, string? bankName, string? shebaNumber)
    {
        var g = await _db.PaymentGateways.FindAsync(id);
        if (g is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(title))
            g.Title = title.Trim();
        g.Description = description?.Trim();
        g.IsEnabled = isEnabled;
        g.IsSandbox = isSandbox;
        g.SortOrder = sortOrder;
        g.MerchantId = merchantId?.Trim();
        g.CardNumber = cardNumber?.Trim();
        g.CardHolder = cardHolder?.Trim();
        g.BankName = bankName?.Trim();
        g.ShebaNumber = shebaNumber?.Trim();

        await _db.SaveChangesAsync();
        TempData["Ok"] = $"تنظیمات ماژول «{g.Title}» با موفقیت ذخیره شد";
        return RedirectToAction(nameof(Index));
    }
}
