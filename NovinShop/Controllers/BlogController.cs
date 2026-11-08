using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovinShop.Data;
using NovinShop.Models;

namespace NovinShop.Controllers;

public class BlogController : Controller
{
    private readonly AppDbContext _db;
    public BlogController(AppDbContext db) => _db = db;

    [HttpGet("/blog")]
    public async Task<IActionResult> Index(string? cat = null, string? q = null, int page = 1)
    {
        const int pageSize = 9;
        var query = _db.BlogPosts.AsNoTracking().Where(p => p.IsPublished);

        if (!string.IsNullOrWhiteSpace(cat))
            query = query.Where(p => p.Category == cat);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Title.Contains(q) || (p.Summary != null && p.Summary.Contains(q)));

        var categories = await _db.BlogPosts.AsNoTracking()
            .Where(p => p.IsPublished)
            .Select(p => p.Category)
            .Distinct()
            .ToListAsync();

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var items = await query
            .OrderByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Categories = categories;
        ViewBag.SelectedCategory = cat;
        ViewBag.Q = q;
        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.Total = total;

        return View(items);
    }

    [HttpGet("/blog/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var post = await _db.BlogPosts.FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);
        if (post is null) return NotFound();

        post.ViewCount++;
        await _db.SaveChangesAsync();

        var related = await _db.BlogPosts.AsNoTracking()
            .Where(p => p.IsPublished && p.Id != post.Id)
            .OrderByDescending(p => p.Category == post.Category)
            .ThenByDescending(p => p.Id)
            .Take(4)
            .ToListAsync();

        var suggestedProducts = await _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.Stock > 0)
            .OrderByDescending(p => p.Id)
            .Take(5)
            .ToListAsync();

        ViewBag.Related = related;
        ViewBag.SuggestedProducts = suggestedProducts;
        return View(post);
    }
}
