using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovinShop.Data;
using NovinShop.Models;

namespace NovinShop.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = AppRoles.CatalogStaff)]
public class ReviewsController : Controller
{
    private readonly AppDbContext _db;
    public ReviewsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string status = "all", string? q = null, int page = 1)
    {
        const int pageSize = 6;
        var query = _db.ProductReviews.AsNoTracking().Include(r => r.Product).AsQueryable();

        if (status == "pending") query = query.Where(r => !r.IsApproved);
        else if (status == "approved") query = query.Where(r => r.IsApproved);

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(r =>
                r.AuthorName.Contains(q) ||
                r.Comment.Contains(q) ||
                (r.Title != null && r.Title.Contains(q)) ||
                (r.Product != null && r.Product.Name.Contains(q)));
        }

        var totalAll = await _db.ProductReviews.CountAsync();
        var pendingCount = await _db.ProductReviews.CountAsync(r => !r.IsApproved);
        var approvedCount = totalAll - pendingCount;

        var totalFiltered = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalFiltered / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var list = await query
            .OrderBy(r => r.IsApproved)
            .ThenByDescending(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Status = status;
        ViewBag.Q = q;
        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalFiltered = totalFiltered;
        ViewBag.TotalAll = totalAll;
        ViewBag.PendingCount = pendingCount;
        ViewBag.ApprovedCount = approvedCount;

        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleApprove(int id, string status = "all", string? q = null, int page = 1)
    {
        var r = await _db.ProductReviews.FindAsync(id);
        if (r is not null)
        {
            r.IsApproved = !r.IsApproved;
            await _db.SaveChangesAsync();
            TempData["Ok"] = r.IsApproved ? "دیدگاه تأیید و در سایت منتشر شد." : "دیدگاه به حالت در انتظار بررسی بازگشت.";
        }
        return RedirectToAction(nameof(Index), new { status, q, page });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int id, string? adminReply, string status = "all", string? q = null, int page = 1)
    {
        var r = await _db.ProductReviews.FindAsync(id);
        if (r is not null)
        {
            r.AdminReply = string.IsNullOrWhiteSpace(adminReply) ? null : adminReply.Trim()[..Math.Min(adminReply.Trim().Length, 600)];
            r.IsApproved = true;
            await _db.SaveChangesAsync();
            TempData["Ok"] = "پاسخ پشتیبانی ذخیره و دیدگاه منتشر شد.";
        }
        return RedirectToAction(nameof(Index), new { status, q, page });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string status = "all", string? q = null, int page = 1)
    {
        var r = await _db.ProductReviews.FindAsync(id);
        if (r is not null)
        {
            _db.ProductReviews.Remove(r);
            await _db.SaveChangesAsync();
            TempData["Ok"] = "دیدگاه حذف شد.";
        }
        return RedirectToAction(nameof(Index), new { status, q, page });
    }
}
