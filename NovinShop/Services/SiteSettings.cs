using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NovinShop.Data;

namespace NovinShop.Services;

// خواندن متن‌ها و تنظیمات سایت (با کش). برای افزودن تنظیم جدید فقط یک خط به Defs اضافه کنید.
public class SiteSettings
{
    public record Def(string Key, string Group, string Label, string Default, bool Multiline = false);

    public static readonly Def[] Defs =
    {
        new("site.name", "عمومی", "نام فروشگاه", "نوین‌شاپ"),
        new("theme.primary", "ظاهر سایت", "رنگ اصلی سایت (کد هگز، مثل #4f46e5)", "#4f46e5"),
        new("site.description", "عمومی", "توضیحات پیش‌فرض سئو (متای سایت)", "فروشگاه اینترنتی نوین‌شاپ؛ خرید مطمئن با ضمانت اصالت و ارسال سریع", true),
        new("top.text", "نوار بالای سایت", "متن نوار بالا (خالی = مخفی)", "🚚 ارسال رایگان برای خرید بالای ۱٬۰۰۰٬۰۰۰ تومان"),
        new("hero.title", "صفحه‌ی اصلی", "عنوان بنر پیش‌فرض (وقتی اسلایدی ندارید)", "خرید مطمئن، با بهترین قیمت و ارسال سریع"),
        new("hero.text", "صفحه‌ی اصلی", "متن زیر عنوان بنر پیش‌فرض", "کالاهای اصل با ضمانت بازگشت ۷ روزه؛ همین حالا از پیشنهادهای ویژه استفاده کنید.", true),
        new("hero.button", "صفحه‌ی اصلی", "متن دکمه‌ی بنر پیش‌فرض", "مشاهده‌ی محصولات"),
        new("home.categories", "صفحه‌ی اصلی", "تیتر بخش دسته‌بندی‌ها", "دسته‌بندی‌ها"),
        new("home.deals", "صفحه‌ی اصلی", "تیتر بخش تخفیف‌ها", "پیشنهادهای ویژه"),
        new("deals.ends", "صفحه‌ی اصلی", "پایان پیشنهاد ویژه برای شمارش معکوس (مثل 2026-11-05 23:59؛ خالی = بدون شمارنده)", ""),
        new("home.best", "صفحه‌ی اصلی", "تیتر بخش پرفروش‌ها", "پرفروش‌ترین‌ها"),
        new("home.latest", "صفحه‌ی اصلی", "تیتر بخش جدیدترین‌ها", "جدیدترین محصولات"),
        new("contact.phone", "فوتر و تماس", "تلفن پشتیبانی", "پشتیبانی: ۰۲۱-۱۲۳۴۵۶۷۸"),
        new("footer.text", "فوتر و تماس", "متن فوتر", "© نوین‌شاپ — تمامی حقوق محفوظ است"),
    };

    private const string CacheKey = "site-settings";
    private const string NavCacheKey = "site-nav";
    private readonly IMemoryCache _cache;
    private readonly IServiceScopeFactory _scopes;
    public SiteSettings(IMemoryCache cache, IServiceScopeFactory scopes) { _cache = cache; _scopes = scopes; }

    private Dictionary<string, string> All() => _cache.GetOrCreate(CacheKey, e =>
    {
        e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = db.SiteSettings.AsNoTracking().ToDictionary(x => x.Key, x => x.Value);
        return Defs.ToDictionary(d => d.Key, d => stored.TryGetValue(d.Key, out var v) ? v : d.Default);
    })!;

    public (List<Models.Category> Categories, List<Models.ContentPage> FooterPages) GetNav() => _cache.GetOrCreate(NavCacheKey, e =>
    {
        e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cats = db.Categories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToList();
        var pages = db.ContentPages.AsNoTracking().Where(p => p.IsActive && p.ShowInFooter).OrderBy(p => p.SortOrder).ToList();
        return (cats, pages);
    });

    public string Get(string key) => All().TryGetValue(key, out var v) ? v : "";
    public void Invalidate() { _cache.Remove(CacheKey); _cache.Remove(NavCacheKey); }
}
