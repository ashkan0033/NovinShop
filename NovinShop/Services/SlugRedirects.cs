using Microsoft.EntityFrameworkCore;
using NovinShop.Data;
using NovinShop.Models;

namespace NovinShop.Services;

public static class SlugRedirects
{
    // قبل از SaveChanges صدا زده شود؛ خودش ذخیره نمی‌کند
    public static async Task RecordAsync(AppDbContext db, string type, string oldSlug, string newSlug)
    {
        if (string.IsNullOrEmpty(oldSlug) || oldSlug == newSlug) return;

        // ریدایرکت‌های قبلی که به آدرس فعلی می‌رفتند، مستقیم به آدرس جدید بروند (بدون زنجیره)
        foreach (var r in await db.SlugRedirects.Where(r => r.Type == type && r.NewSlug == oldSlug).ToListAsync())
            r.NewSlug = newSlug;

        // اگر به آدرسی برگشتیم که قبلاً ریدایرکت داشت، آن ریدایرکت حذف شود
        db.SlugRedirects.RemoveRange(await db.SlugRedirects.Where(r => r.Type == type && r.OldSlug == newSlug).ToListAsync());

        db.SlugRedirects.Add(new SlugRedirect { Type = type, OldSlug = oldSlug, NewSlug = newSlug });
    }

    public static Task<SlugRedirect?> FindAsync(AppDbContext db, string type, string slug) =>
        db.SlugRedirects.AsNoTracking().FirstOrDefaultAsync(r => r.Type == type && r.OldSlug == slug);
}
