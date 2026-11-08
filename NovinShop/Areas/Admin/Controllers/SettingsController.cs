using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovinShop.Data;
using NovinShop.Models;
using NovinShop.Services;

namespace NovinShop.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "Admin")]
public class SettingsController : Controller
{
    private readonly AppDbContext _db;
    private readonly SiteSettings _site;
    public SettingsController(AppDbContext db, SiteSettings site) { _db = db; _site = site; }

    [HttpGet]
    public IActionResult Index() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(Dictionary<string, string?> s)
    {
        var stored = await _db.SiteSettings.ToDictionaryAsync(x => x.Key);
        foreach (var d in SiteSettings.Defs)          // فقط کلیدهای تعریف‌شده پذیرفته می‌شوند
        {
            if (!s.TryGetValue(d.Key, out var v)) continue;
            v = (v ?? "").Trim();
            if (v.Length > 1000) v = v[..1000];
            if (stored.TryGetValue(d.Key, out var row)) row.Value = v;
            else _db.SiteSettings.Add(new SiteSetting { Key = d.Key, Value = v });
        }
        await _db.SaveChangesAsync();
        _site.Invalidate();
        TempData["Ok"] = "تنظیمات ذخیره شد و روی سایت اعمال شد";
        return RedirectToAction(nameof(Index));
    }
}
