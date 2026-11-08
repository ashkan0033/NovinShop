using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NovinShop.Data;
using NovinShop.Models;

namespace NovinShop.Services;

// سبد خرید در Session: {شناسه محصول: تعداد}. قیمت‌ها هرگز اینجا ذخیره نمی‌شوند.
public class CartService
{
    private const string Key = "cart";
    private const string CouponKey = "cart_coupon";
    private readonly IHttpContextAccessor _http;
    public CartService(IHttpContextAccessor http) => _http = http;

    private ISession Session => _http.HttpContext!.Session;

    public Dictionary<int, int> Get()
    {
        var json = Session.GetString(Key);
        return string.IsNullOrEmpty(json)
            ? new Dictionary<int, int>()
            : JsonSerializer.Deserialize<Dictionary<int, int>>(json) ?? new Dictionary<int, int>();
    }

    private void Save(Dictionary<int, int> c) => Session.SetString(Key, JsonSerializer.Serialize(c));

    public int Count => Get().Values.Sum();

    public void Add(int id, int qty)
    {
        var c = Get();
        c[id] = Math.Clamp(c.GetValueOrDefault(id) + qty, 1, 10);
        Save(c);
    }

    public void Set(int id, int qty)
    {
        var c = Get();
        if (qty < 1) c.Remove(id); else c[id] = Math.Min(qty, 10);
        Save(c);
    }

    public void Remove(int id) { var c = Get(); c.Remove(id); Save(c); }

    public string? GetCoupon() => Session.GetString(CouponKey);

    public void SetCoupon(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            Session.Remove(CouponKey);
        else
            Session.SetString(CouponKey, code.Trim().ToUpperInvariant());
    }

    public void Clear()
    {
        Session.Remove(Key);
        Session.Remove(CouponKey);
    }

    public async Task<CartViewModel> BuildViewModelAsync(AppDbContext db)
    {
        var cart = Get();
        var ids = cart.Keys.ToList();
        var products = await db.Products.AsNoTracking()
            .Where(p => ids.Contains(p.Id) && p.IsActive && p.Stock > 0).ToListAsync();

        foreach (var gone in ids.Except(products.Select(p => p.Id))) Remove(gone);

        var lines = products.Select(p => new CartLine(p, Math.Min(cart[p.Id], p.Stock))).ToList();
        var subtotal = lines.Sum(l => l.Total);

        var code = GetCoupon();
        if (string.IsNullOrWhiteSpace(code) || lines.Count == 0)
            return new CartViewModel(lines);

        var coupon = await db.Coupons.AsNoTracking().FirstOrDefaultAsync(c => c.Code == code);
        if (coupon is null)
        {
            SetCoupon(null);
            return new CartViewModel(lines);
        }

        var (ok, discount, err) = coupon.CalculateDiscount(subtotal, DateTime.UtcNow);
        if (!ok)
        {
            return new CartViewModel(lines, coupon.Code, coupon.Title, 0, err);
        }

        return new CartViewModel(lines, coupon.Code, coupon.Title, discount, null);
    }
}
