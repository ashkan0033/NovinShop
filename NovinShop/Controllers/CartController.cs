using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovinShop.Data;
using NovinShop.Models;
using NovinShop.Services;

namespace NovinShop.Controllers;

public class CartController : Controller
{
    private readonly AppDbContext _db;
    private readonly CartService _cart;
    public CartController(AppDbContext db, CartService cart) { _db = db; _cart = cart; }

    [HttpGet("/cart")]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "سبد خرید | نوین‌شاپ";
        ViewData["Robots"] = "noindex,nofollow";
        return View(await BuildAsync());
    }

    [HttpPost("/cart/add"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int id, int qty = 1, string? returnUrl = null)
    {
        var p = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (p is null || p.Stock < 1)
            TempData["Err"] = "این محصول در حال حاضر موجود نیست";
        else
        {
            _cart.Add(id, Math.Clamp(qty, 1, 10));
            TempData["Ok"] = "به سبد خرید اضافه شد";
        }
        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction(nameof(Index));
    }

    [HttpPost("/cart/update"), ValidateAntiForgeryToken]
    public IActionResult Update(int id, int qty)
    {
        _cart.Set(id, qty);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/cart/remove"), ValidateAntiForgeryToken]
    public IActionResult Remove(int id)
    {
        _cart.Remove(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/cart/coupon"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyCoupon(string? code, string? returnUrl = null)
    {
        var normalized = (code ?? "").Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            TempData["Err"] = "لطفاً کد تخفیف را وارد کنید.";
            return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction(nameof(Index));
        }

        var coupon = await _db.Coupons.AsNoTracking().FirstOrDefaultAsync(c => c.Code == normalized);
        if (coupon is null)
        {
            TempData["Err"] = $"کد تخفیف «{normalized}» معتبر نیست.";
            return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction(nameof(Index));
        }

        var vm = await _cart.BuildViewModelAsync(_db);
        var (ok, discount, err) = coupon.CalculateDiscount(vm.Subtotal, DateTime.UtcNow);
        if (!ok)
        {
            TempData["Err"] = err;
            return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction(nameof(Index));
        }

        _cart.SetCoupon(coupon.Code);
        TempData["Ok"] = $"کد تخفیف «{coupon.Code}» ({coupon.Title}) اعمال شد و {Fmt.Toman(discount)} از مبلغ سفارش شما کسر گردید.";
        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction(nameof(Index));
    }

    [HttpPost("/cart/coupon/remove"), ValidateAntiForgeryToken]
    public IActionResult RemoveCoupon(string? returnUrl = null)
    {
        _cart.SetCoupon(null);
        TempData["Ok"] = "کد تخفیف از سبد خرید حذف شد.";
        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction(nameof(Index));
    }

    // قیمت و موجودی همیشه از دیتابیس خوانده می‌شود، نه از سبد
    private Task<CartViewModel> BuildAsync() => _cart.BuildViewModelAsync(_db);
}
