using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovinShop.Data;
using NovinShop.Models;

namespace NovinShop.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = AppRoles.OrderAndFinanceStaff)]
public class CouponsController : Controller
{
    private readonly AppDbContext _db;
    public CouponsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View(await _db.Coupons.AsNoTracking().OrderByDescending(c => c.Id).ToListAsync());

    public IActionResult Create() =>
        View("Form", new Coupon { IsPercent = true, Value = 15, IsActive = true });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Coupon m)
    {
        m.Code = (m.Code ?? "").Trim().ToUpperInvariant();
        if (await _db.Coupons.AnyAsync(c => c.Code == m.Code))
            ModelState.AddModelError("Code", "این کد تخفیف قبلاً ثبت شده است.");

        if (m.IsPercent && (m.Value < 1 || m.Value > 100))
            ModelState.AddModelError("Value", "درصد تخفیف باید بین ۱ تا ۱۰۰ باشد.");

        if (!ModelState.IsValid) return View("Form", m);

        m.CreatedAt = DateTime.UtcNow;
        _db.Coupons.Add(m);
        await _db.SaveChangesAsync();
        TempData["Ok"] = $"کد تخفیف «{m.Code}» با موفقیت ایجاد شد.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var c = await _db.Coupons.FindAsync(id);
        return c is null ? NotFound() : View("Form", c);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Coupon m)
    {
        var c = await _db.Coupons.FindAsync(id);
        if (c is null) return NotFound();

        m.Code = (m.Code ?? "").Trim().ToUpperInvariant();
        if (await _db.Coupons.AnyAsync(x => x.Code == m.Code && x.Id != id))
            ModelState.AddModelError("Code", "این کد تخفیف قبلاً برای کوپن دیگری ثبت شده است.");

        if (m.IsPercent && (m.Value < 1 || m.Value > 100))
            ModelState.AddModelError("Value", "درصد تخفیف باید بین ۱ تا ۱۰۰ باشد.");

        if (!ModelState.IsValid) return View("Form", m);

        c.Code = m.Code;
        c.Title = m.Title.Trim();
        c.IsPercent = m.IsPercent;
        c.Value = m.Value;
        c.MaxDiscount = m.IsPercent ? m.MaxDiscount : null;
        c.MinOrderAmount = Math.Max(0, m.MinOrderAmount);
        c.UsageLimit = m.UsageLimit is > 0 ? m.UsageLimit : null;
        c.ExpiresAt = m.ExpiresAt;
        c.IsActive = m.IsActive;

        await _db.SaveChangesAsync();
        TempData["Ok"] = $"کد تخفیف «{c.Code}» بروزرسانی شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var c = await _db.Coupons.FindAsync(id);
        if (c is not null)
        {
            c.IsActive = !c.IsActive;
            await _db.SaveChangesAsync();
            TempData["Ok"] = c.IsActive ? $"کد «{c.Code}» فعال شد." : $"کد «{c.Code}» غیرفعال شد.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Coupons.FindAsync(id);
        if (c is not null)
        {
            _db.Coupons.Remove(c);
            await _db.SaveChangesAsync();
            TempData["Ok"] = $"کد تخفیف «{c.Code}» حذف شد.";
        }
        return RedirectToAction(nameof(Index));
    }
}
