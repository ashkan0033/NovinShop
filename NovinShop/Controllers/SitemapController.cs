using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovinShop.Data;

namespace NovinShop.Controllers;

public class SitemapController : Controller
{
    private readonly AppDbContext _db;
    public SitemapController(AppDbContext db) => _db = db;

    [HttpGet("/sitemap.xml")]
    public async Task<IActionResult> Sitemap()
    {
        var host = $"{Request.Scheme}://{Request.Host}";
        var urls = new List<(string Path, string Freq, string Prio)> { ("/", "daily", "1.0"), ("/products", "daily", "0.9") };

        urls.AddRange((await _db.Categories.AsNoTracking().Where(c => c.IsActive).Select(c => c.Slug).ToListAsync())
            .Select(s => ("/category/" + Uri.EscapeDataString(s), "daily", "0.8")));
        urls.AddRange((await _db.Products.AsNoTracking().Where(p => p.IsActive).Select(p => p.Slug).ToListAsync())
            .Select(s => ("/product/" + Uri.EscapeDataString(s), "weekly", "0.7")));
        urls.AddRange((await _db.ContentPages.AsNoTracking().Where(p => p.IsActive).Select(p => p.Slug).ToListAsync())
            .Select(s => ("/page/" + Uri.EscapeDataString(s), "monthly", "0.4")));

        var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n");
        foreach (var u in urls)
            sb.Append("  <url><loc>").Append(WebUtility.HtmlEncode(host + u.Path)).Append("</loc><changefreq>")
              .Append(u.Freq).Append("</changefreq><priority>").Append(u.Prio).Append("</priority></url>\n");
        sb.Append("</urlset>");
        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }

    [HttpGet("/robots.txt")]
    public IActionResult Robots()
    {
        var host = $"{Request.Scheme}://{Request.Host}";
        var txt = string.Join("\n", new[]
        {
            "User-agent: *",
            "Disallow: /admin", "Disallow: /account", "Disallow: /cart", "Disallow: /checkout",
            "Disallow: /pay/", "Disallow: /order/", "Disallow: /track", "Disallow: /*?q=",
            "", $"Sitemap: {host}/sitemap.xml"
        });
        return Content(txt, "text/plain", Encoding.UTF8);
    }
}
