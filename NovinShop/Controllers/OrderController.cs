using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovinShop.Data;
using NovinShop.Models;
using NovinShop.Services;

namespace NovinShop.Controllers;

public class OrderController : Controller
{
    private static readonly string[] AllowedReceiptExt = { ".jpg", ".jpeg", ".png", ".webp" };
    private readonly AppDbContext _db;
    private readonly CartService _cart;
    private readonly IWebHostEnvironment _env;
    private readonly ZarinpalService _zarinpal;

    public OrderController(AppDbContext db, CartService cart, IWebHostEnvironment env, ZarinpalService zarinpal)
    {
        _db = db; _cart = cart; _env = env; _zarinpal = zarinpal;
    }

    // ───────────── ثبت سفارش (خرید مهمان + انتخاب ماژول پرداخت) ─────────────

    [HttpGet("/checkout")]
    public async Task<IActionResult> Checkout()
    {
        var vm = await BuildCartAsync();
        if (vm.Lines.Count == 0) return RedirectToAction("Index", "Cart");
        var gateways = await GetEnabledGatewaysAsync();
        var form = new CheckoutForm { PaymentMethod = gateways.FirstOrDefault()?.Key ?? "card_to_card" };
        return View(new CheckoutPage(vm, form, gateways));
    }

    [HttpPost("/checkout"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout([Bind(Prefix = "Form")] CheckoutForm form)
    {
        var cart = _cart.Get();
        var vm = await BuildCartAsync();
        if (vm.Lines.Count == 0) return RedirectToAction("Index", "Cart");
        var gateways = await GetEnabledGatewaysAsync();

        // اگر موجودی یا وضعیت محصولی بعد از افزودن به سبد عوض شده باشد
        if (vm.Lines.Count != cart.Count || vm.Lines.Any(l => l.Qty != cart[l.Product.Id]))
        {
            foreach (var gone in cart.Keys.Except(vm.Lines.Select(l => l.Product.Id))) _cart.Remove(gone);
            foreach (var l in vm.Lines) _cart.Set(l.Product.Id, l.Qty);
            ModelState.AddModelError("", "موجودی یا وضعیت برخی محصولات تغییر کرده است؛ خلاصه‌ی سفارش را بررسی کنید");
        }

        form.Phone = NormalizeDigits(form.Phone ?? "").Trim();
        ModelState.Remove("Form.Phone");
        if (!Regex.IsMatch(form.Phone, @"^09\d{9}$"))
            ModelState.AddModelError("Form.Phone", "شماره موبایل معتبر نیست (مثال: 09123456789)");

        var selectedGw = gateways.FirstOrDefault(g => g.Key == form.PaymentMethod) ?? gateways.FirstOrDefault();
        if (selectedGw is null)
            ModelState.AddModelError("", "در حال حاضر هیچ روش پرداختی فعال نیست. لطفاً با پشتیبانی تماس بگیرید.");

        if (!ModelState.IsValid) return View("Checkout", new CheckoutPage(vm, form, gateways));

        // کم کردن موجودی به‌صورت اتمیک؛ اگر هر محصولی کم بیاید، کل عملیات برگردانده می‌شود
        await using var tx = await _db.Database.BeginTransactionAsync();
        foreach (var l in vm.Lines)
        {
            var id = l.Product.Id; var qty = l.Qty;
            var n = await _db.Products.Where(p => p.Id == id && p.IsActive && p.Stock >= qty)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - qty));
            if (n == 0)
            {
                await tx.RollbackAsync();
                ModelState.AddModelError("", $"موجودی «{l.Product.Name}» همین حالا تمام شد");
                return View("Checkout", new CheckoutPage(await BuildCartAsync(), form, gateways));
            }
        }

        string? appliedCouponCode = null;
        long appliedDiscount = 0;
        if (!string.IsNullOrWhiteSpace(vm.CouponCode) && vm.Discount > 0)
        {
            var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code == vm.CouponCode);
            if (coupon is not null)
            {
                var (ok, disc, err) = coupon.CalculateDiscount(vm.Subtotal, DateTime.UtcNow);
                if (!ok)
                {
                    await tx.RollbackAsync();
                    _cart.SetCoupon(null);
                    ModelState.AddModelError("", err ?? "کد تخفیف دیگر معتبر نیست.");
                    return View("Checkout", new CheckoutPage(await BuildCartAsync(), form, gateways));
                }

                var updated = await _db.Coupons
                    .Where(c => c.Id == coupon.Id && c.IsActive && (c.UsageLimit == null || c.UsedCount < c.UsageLimit))
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsedCount, c => c.UsedCount + 1));
                if (updated == 0)
                {
                    await tx.RollbackAsync();
                    _cart.SetCoupon(null);
                    ModelState.AddModelError("", "ظرفیت استفاده از این کد تخفیف همین لحظه به پایان رسید.");
                    return View("Checkout", new CheckoutPage(await BuildCartAsync(), form, gateways));
                }

                appliedCouponCode = coupon.Code;
                appliedDiscount = disc;
            }
        }

        var finalTotal = Math.Max(0, vm.Subtotal - appliedDiscount) + vm.Shipping;
        var order = new Order
        {
            Code = await NewCodeAsync(),
            CustomerName = form.Name.Trim(),
            Phone = form.Phone,
            Address = form.Address.Trim(),
            PaymentMethod = selectedGw!.Key,
            Subtotal = vm.Subtotal,
            DiscountAmount = appliedDiscount,
            CouponCode = appliedCouponCode,
            Shipping = vm.Shipping,
            Total = finalTotal,
            Items = vm.Lines.Select(l => new OrderItem
            {
                ProductId = l.Product.Id, ProductName = l.Product.Name,
                UnitPrice = l.Product.Price,
                UnitCost = l.Product.CostPrice > 0 ? l.Product.CostPrice : (int)Math.Round(l.Product.Price * 0.72),
                Quantity = l.Qty
            }).ToList()
        };
        _db.Orders.Add(order);

        // ثبت خودکار حواله خروج از انبار در کاردکس انبارداری
        foreach (var l in vm.Lines)
        {
            _db.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = l.Product.Id,
                Type = InventoryTxType.SaleOut,
                QuantityChange = -l.Qty,
                StockAfter = Math.Max(0, l.Product.Stock - l.Qty),
                UnitCost = l.Product.CostPrice > 0 ? l.Product.CostPrice : (int)Math.Round(l.Product.Price * 0.72),
                Reference = order.Code,
                Note = $"کسر خودکار بابت ثبت سفارش {order.Code} ({order.CustomerName})",
                CreatedBy = "سیستم فروش",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        _cart.Clear();
        Remember(order.Code);
        return RedirectToAction(nameof(Pay), new { code = order.Code, method = order.PaymentMethod });
    }

    // ───────────── صفحه‌ی پرداخت (زرین‌پال / کارت به کارت / شبیه‌ساز) ─────────────

    [HttpGet("/pay/{code}")]
    public async Task<IActionResult> Pay(string code, string? method = null)
    {
        if (!Mine().Contains(code)) return RedirectToAction(nameof(Track));
        var o = await _db.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.Code == code);
        if (o is null) return NotFound();
        if (o.Status != OrderStatus.PendingPayment) return RedirectToAction(nameof(Details), new { code });

        var gateways = await GetEnabledGatewaysAsync();
        if (!string.IsNullOrWhiteSpace(method) && gateways.Any(g => g.Key == method))
        {
            o.PaymentMethod = method;
            await _db.SaveChangesAsync();
        }
        else if (string.IsNullOrWhiteSpace(o.PaymentMethod) || !gateways.Any(g => g.Key == o.PaymentMethod))
        {
            o.PaymentMethod = gateways.FirstOrDefault()?.Key ?? "card_to_card";
            await _db.SaveChangesAsync();
        }

        ViewBag.Gateways = gateways;
        ViewData["Robots"] = "noindex,nofollow";
        return View(o);
    }

    // شروع پرداخت آنلاین زرین‌پال
    [HttpPost("/pay/{code}/zarinpal"), ValidateAntiForgeryToken]
    public async Task<IActionResult> StartZarinpal(string code)
    {
        if (!Mine().Contains(code)) return RedirectToAction(nameof(Track));
        var o = await _db.Orders.FirstOrDefaultAsync(x => x.Code == code);
        if (o is null) return NotFound();
        if (o.Status != OrderStatus.PendingPayment) return RedirectToAction(nameof(Details), new { code });

        var gw = await _db.PaymentGateways.AsNoTracking().FirstOrDefaultAsync(g => g.Key == "zarinpal" && g.IsEnabled);
        if (gw is null)
        {
            TempData["Err"] = "درگاه زرین‌پال در حال حاضر غیرفعال است.";
            return RedirectToAction(nameof(Pay), new { code });
        }

        var callbackUrl = $"{Request.Scheme}://{Request.Host}/pay/zarinpal/callback?code={Uri.EscapeDataString(o.Code)}";
        var res = await _zarinpal.RequestAsync(
            gw.MerchantId ?? "", gw.IsSandbox, o.Total, $"پرداخت سفارش {o.Code}", callbackUrl, o.Phone);

        if (!res.Ok || string.IsNullOrEmpty(res.StartPayUrl))
        {
            TempData["Err"] = res.Error ?? "خطا در اتصال به درگاه زرین‌پال";
            return RedirectToAction(nameof(Pay), new { code });
        }

        o.PaymentMethod = "zarinpal";
        o.Authority = res.Authority;
        await _db.SaveChangesAsync();
        return Redirect(res.StartPayUrl);
    }

    // بازگشت از درگاه زرین‌پال (Callback)
    [HttpGet("/pay/zarinpal/callback")]
    public async Task<IActionResult> ZarinpalCallback(string code, string? Authority, string? Status)
    {
        var o = await _db.Orders.FirstOrDefaultAsync(x => x.Code == code);
        if (o is null) return NotFound();
        Remember(o.Code);

        if (o.Status == OrderStatus.Paid || o.Status == OrderStatus.Shipped)
            return RedirectToAction(nameof(Details), new { code = o.Code });

        if (!string.Equals(Status, "OK", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(Authority))
        {
            TempData["Err"] = "پرداخت توسط شما لغو شد یا ناموفق بود. می‌توانید مجدداً تلاش کنید یا روش پرداخت را تغییر دهید.";
            return RedirectToAction(nameof(Pay), new { code = o.Code });
        }

        var gw = await _db.PaymentGateways.AsNoTracking().FirstOrDefaultAsync(g => g.Key == "zarinpal");
        if (gw is null) return RedirectToAction(nameof(Pay), new { code = o.Code });

        var verify = await _zarinpal.VerifyAsync(gw.MerchantId ?? "", gw.IsSandbox, o.Total, Authority);
        if (verify.Ok)
        {
            o.Status = OrderStatus.Paid;
            o.PaidAt = DateTime.UtcNow;
            o.Authority = Authority;
            o.PaymentRef = "ZP-" + verify.RefId;
            await _db.SaveChangesAsync();
            TempData["Ok"] = $"پرداخت با موفقیت انجام شد. کد رهگیری زرین‌پال: {verify.RefId}";
            return RedirectToAction(nameof(Details), new { code = o.Code });
        }

        TempData["Err"] = verify.Error ?? "تراکنش توسط زرین‌پال تأیید نشد. در صورت کسر وجه، مبلغ تا ۷۲ ساعت به حسابتان برمی‌گردد.";
        return RedirectToAction(nameof(Pay), new { code = o.Code });
    }

    // ثبت فیش واریزی کارت به کارت توسط مشتری
    [HttpPost("/pay/{code}/receipt"), ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitReceipt(string code, IFormFile? receiptImage, string? receiptNote)
    {
        if (!Mine().Contains(code)) return RedirectToAction(nameof(Track));
        var o = await _db.Orders.FirstOrDefaultAsync(x => x.Code == code);
        if (o is null) return NotFound();
        if (o.Status != OrderStatus.PendingPayment) return RedirectToAction(nameof(Details), new { code });

        if (receiptImage is null || receiptImage.Length == 0)
        {
            TempData["Err"] = "لطفاً تصویر فیش یا اسکرین‌شات واریز را انتخاب کنید.";
            return RedirectToAction(nameof(Pay), new { code, method = "card_to_card" });
        }

        var ext = Path.GetExtension(receiptImage.FileName).ToLowerInvariant();
        if (!AllowedReceiptExt.Contains(ext))
        {
            TempData["Err"] = "فرمت تصویر فیش باید JPG، PNG یا WebP باشد.";
            return RedirectToAction(nameof(Pay), new { code, method = "card_to_card" });
        }
        if (receiptImage.Length > 3 * 1024 * 1024)
        {
            TempData["Err"] = "حجم تصویر فیش نباید بیشتر از ۳ مگابایت باشد.";
            return RedirectToAction(nameof(Pay), new { code, method = "card_to_card" });
        }

        var dir = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "receipts");
        Directory.CreateDirectory(dir);
        var fileName = $"{o.Code.ToLowerInvariant()}-{Guid.NewGuid():N}{ext}";
        await using (var fs = System.IO.File.Create(Path.Combine(dir, fileName)))
        {
            await receiptImage.CopyToAsync(fs);
        }

        o.PaymentMethod = "card_to_card";
        o.ReceiptImagePath = "/uploads/receipts/" + fileName;
        o.ReceiptNote = string.IsNullOrWhiteSpace(receiptNote) ? null : receiptNote.Trim()[..Math.Min(receiptNote.Trim().Length, 250)];
        o.AdminNote = null;
        o.Status = OrderStatus.PendingApproval;
        await _db.SaveChangesAsync();

        TempData["Ok"] = "فیش واریزی شما با موفقیت ثبت شد و پس از بررسی توسط مدیریت، وضعیت سفارش به «پرداخت‌شده» تغییر خواهد کرد.";
        return RedirectToAction(nameof(Details), new { code = o.Code });
    }

    // شبیه‌ساز درگاه آزمایشی داخلی
    [HttpPost("/pay/{code}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(string code, bool success)
    {
        if (!Mine().Contains(code)) return RedirectToAction(nameof(Track));
        var o = await _db.Orders.FirstOrDefaultAsync(x => x.Code == code);
        if (o is null) return NotFound();

        if (o.Status == OrderStatus.PendingPayment)
        {
            if (success)
            {
                o.PaymentMethod = "simulator";
                o.Status = OrderStatus.Paid;
                o.PaidAt = DateTime.UtcNow;
                o.PaymentRef = "SIM-" + RandomNumberGenerator.GetInt32(100000, 999999);
                await _db.SaveChangesAsync();
                TempData["Ok"] = "پرداخت آزمایشی با موفقیت انجام شد";
            }
            else
            {
                TempData["Err"] = "پرداخت ناموفق بود یا لغو شد. می‌توانید دوباره تلاش کنید.";
                return RedirectToAction(nameof(Pay), new { code });
            }
        }
        return RedirectToAction(nameof(Details), new { code });
    }

    // ───────────── مشاهده و پیگیری سفارش ─────────────

    [HttpGet("/order/{code}")]
    public async Task<IActionResult> Details(string code)
    {
        if (!Mine().Contains(code) && !User.IsInRole("Admin")) return RedirectToAction(nameof(Track));
        var o = await _db.Orders.AsNoTracking().Include(x => x.Items).FirstOrDefaultAsync(x => x.Code == code);
        if (o is null) return NotFound();
        ViewData["Robots"] = "noindex,nofollow";
        return View(o);
    }

    [HttpGet("/order/{code}/invoice")]
    public async Task<IActionResult> Invoice(string code, string mode = "invoice")
    {
        if (!Mine().Contains(code) && !User.IsInRole("Admin")) return RedirectToAction(nameof(Track));
        var o = await _db.Orders.AsNoTracking().Include(x => x.Items).FirstOrDefaultAsync(x => x.Code == code);
        if (o is null) return NotFound();
        ViewBag.IsAdmin = User.IsInRole("Admin");
        ViewBag.Mode = mode;
        return View("Invoice", o);
    }

    [HttpGet("/track")]
    public IActionResult Track()
    {
        ViewData["Title"] = "پیگیری سفارش | نوین‌شاپ";
        return View(new TrackForm());
    }

    [HttpPost("/track"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Track(TrackForm f)
    {
        var code = (f.Code ?? "").Trim().ToUpperInvariant();
        var phone = NormalizeDigits(f.Phone ?? "").Trim();
        var found = await _db.Orders.AsNoTracking().AnyAsync(x => x.Code == code && x.Phone == phone);
        if (!found)
        {
            ModelState.AddModelError("", "سفارشی با این کد و شماره موبایل پیدا نشد");
            return View(f);
        }
        Remember(code);
        return RedirectToAction(nameof(Details), new { code });
    }

    // ───────────── کمکی‌ها ─────────────

    private Task<List<PaymentGateway>> GetEnabledGatewaysAsync() =>
        _db.PaymentGateways.AsNoTracking().Where(g => g.IsEnabled).OrderBy(g => g.SortOrder).ToListAsync();

    private Task<CartViewModel> BuildCartAsync() => _cart.BuildViewModelAsync(_db);

    private async Task<string> NewCodeAsync()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        while (true)
        {
            var code = "NS-" + new string(Enumerable.Range(0, 8).Select(_ => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)]).ToArray());
            if (!await _db.Orders.AnyAsync(o => o.Code == code)) return code;
        }
    }

    private HashSet<string> Mine() =>
        JsonSerializer.Deserialize<HashSet<string>>(HttpContext.Session.GetString("orders") ?? "[]") ?? new();

    private void Remember(string code)
    {
        var s = Mine(); s.Add(code);
        HttpContext.Session.SetString("orders", JsonSerializer.Serialize(s));
    }

    private static string NormalizeDigits(string s) => string.Concat(s.Select(c =>
        c is >= '۰' and <= '۹' ? (char)('0' + (c - '۰')) :
        c is >= '٠' and <= '٩' ? (char)('0' + (c - '٠')) : c));
}
