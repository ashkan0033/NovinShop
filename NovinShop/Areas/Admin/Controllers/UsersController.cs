using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovinShop.Models;

namespace NovinShop.Areas.Admin.Controllers;

public record StaffUserRow(string Id, string Email, string Role, bool IsLockedOut);

[Area("Admin"), Authorize(Roles = AppRoles.Admin)]
public class UsersController : Controller
{
    private readonly UserManager<IdentityUser> _users;
    private readonly RoleManager<IdentityRole> _roles;
    private readonly SignInManager<IdentityUser> _signIn;

    public UsersController(
        UserManager<IdentityUser> users,
        RoleManager<IdentityRole> roles,
        SignInManager<IdentityUser> signIn)
    {
        _users = users;
        _roles = roles;
        _signIn = signIn;
    }

    public async Task<IActionResult> Index()
    {
        await EnsureRolesExistAsync();
        var allUsers = await _users.Users.OrderBy(u => u.Email).ToListAsync();
        var rows = new List<StaffUserRow>();

        foreach (var u in allUsers)
        {
            var userRoles = await _users.GetRolesAsync(u);
            var primaryRole = userRoles.FirstOrDefault() ?? AppRoles.Editor;
            rows.Add(new StaffUserRow(
                u.Id,
                u.Email ?? u.UserName ?? "",
                primaryRole,
                u.LockoutEnd is not null && u.LockoutEnd > DateTimeOffset.UtcNow));
        }

        var me = await _users.GetUserAsync(User);
        ViewBag.MyEmail = me?.Email ?? User.Identity?.Name ?? "";
        ViewBag.MyId = me?.Id ?? "";
        return View(rows);
    }

    // ۱) ویرایش ایمیل و رمز عبور خودِ مدیر کل
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateMyProfile(string? newEmail, string? currentPassword, string? newPassword)
    {
        var me = await _users.GetUserAsync(User);
        if (me is null) return RedirectToAction(nameof(Index));

        var email = (newEmail ?? "").Trim();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            TempData["Err"] = "لطفاً یک آدرس ایمیل معتبر وارد کنید.";
            return RedirectToAction(nameof(Index));
        }

        if (!string.Equals(me.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _users.FindByEmailAsync(email);
            if (existing is not null && existing.Id != me.Id)
            {
                TempData["Err"] = "این ایمیل قبلاً برای کاربر دیگری ثبت شده است.";
                return RedirectToAction(nameof(Index));
            }
            me.Email = email;
            me.UserName = email;
            me.NormalizedEmail = _users.NormalizeEmail(email);
            me.NormalizedUserName = _users.NormalizeName(email);
            var updateRes = await _users.UpdateAsync(me);
            if (!updateRes.Succeeded)
            {
                TempData["Err"] = string.Join(" — ", updateRes.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }
        }

        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                TempData["Err"] = "برای تغییر رمز عبور خودتان، وارد کردن رمز عبور فعلی الزامی است.";
                return RedirectToAction(nameof(Index));
            }
            var passRes = await _users.ChangePasswordAsync(me, currentPassword, newPassword);
            if (!passRes.Succeeded)
            {
                TempData["Err"] = "رمز عبور فعلی اشتباه است یا رمز جدید حداقل ۸ کاراکتر (شامل حروف بزرگ، کوچک و عدد) نیست.";
                return RedirectToAction(nameof(Index));
            }
        }

        await _signIn.RefreshSignInAsync(me);
        TempData["Ok"] = "اطلاعات ورود شما (ایمیل / رمز عبور) با موفقیت بروزرسانی شد.";
        return RedirectToAction(nameof(Index));
    }

    // ۲) ایجاد کاربر / کارمند جدید با سطح دسترسی مشخص
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStaff(string? email, string? password, string role = AppRoles.Editor)
    {
        await EnsureRolesExistAsync();
        var cleanEmail = (email ?? "").Trim();
        if (string.IsNullOrWhiteSpace(cleanEmail) || !cleanEmail.Contains('@'))
        {
            TempData["Err"] = "ایمیل کاربر جدید معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            TempData["Err"] = "رمز عبور باید حداقل ۸ کاراکتر باشد.";
            return RedirectToAction(nameof(Index));
        }
        if (await _users.FindByEmailAsync(cleanEmail) is not null)
        {
            TempData["Err"] = "کاربری با این ایمیل قبلاً ثبت شده است.";
            return RedirectToAction(nameof(Index));
        }

        var validRole = role is AppRoles.Admin or AppRoles.Editor or AppRoles.Accountant ? role : AppRoles.Editor;
        var user = new IdentityUser { UserName = cleanEmail, Email = cleanEmail, EmailConfirmed = true };
        var res = await _users.CreateAsync(user, password);
        if (!res.Succeeded)
        {
            TempData["Err"] = "خطا در ایجاد کاربر: رمز عبور باید حداقل ۸ کاراکتر و شامل حروف بزرگ، کوچک و عدد باشد (مثال: Staff@12345).";
            return RedirectToAction(nameof(Index));
        }

        await _users.AddToRoleAsync(user, validRole);
        TempData["Ok"] = $"کاربر «{cleanEmail}» با سطح دسترسی «{AppRoles.Label(validRole)}» ایجاد شد.";
        return RedirectToAction(nameof(Index));
    }

    // ۳) تغییر نقش یا ویرایش ایمیل/رمز کارمند توسط مدیر کل
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStaff(string id, string? email, string role, string? newPassword)
    {
        await EnsureRolesExistAsync();
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();

        var me = await _users.GetUserAsync(User);
        var validRole = role is AppRoles.Admin or AppRoles.Editor or AppRoles.Accountant ? role : AppRoles.Editor;

        if (me is not null && user.Id == me.Id && validRole != AppRoles.Admin)
        {
            TempData["Err"] = "شما نمی‌توانید سطح دسترسی مدیر کل را از حساب خودتان بردارید.";
            return RedirectToAction(nameof(Index));
        }

        var cleanEmail = (email ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(cleanEmail) && cleanEmail.Contains('@') && !string.Equals(user.Email, cleanEmail, StringComparison.OrdinalIgnoreCase))
        {
            var dup = await _users.FindByEmailAsync(cleanEmail);
            if (dup is not null && dup.Id != user.Id)
            {
                TempData["Err"] = "این ایمیل قبلاً توسط کاربر دیگری استفاده شده است.";
                return RedirectToAction(nameof(Index));
            }
            user.Email = cleanEmail;
            user.UserName = cleanEmail;
            user.NormalizedEmail = _users.NormalizeEmail(cleanEmail);
            user.NormalizedUserName = _users.NormalizeName(cleanEmail);
            await _users.UpdateAsync(user);
        }

        var currentRoles = await _users.GetRolesAsync(user);
        if (currentRoles.Count > 0)
            await _users.RemoveFromRolesAsync(user, currentRoles);
        await _users.AddToRoleAsync(user, validRole);

        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var passRes = await _users.ResetPasswordAsync(user, token, newPassword);
            if (!passRes.Succeeded)
            {
                TempData["Err"] = "نقش بروزرسانی شد اما رمز جدید معتبر نبود (حداقل ۸ کاراکتر شامل حروف بزرگ، کوچک و عدد).";
                return RedirectToAction(nameof(Index));
            }
        }

        if (me is not null && user.Id == me.Id)
            await _signIn.RefreshSignInAsync(user);

        TempData["Ok"] = $"اطلاعات و سطح دسترسی «{user.Email}» با موفقیت ذخیره شد.";
        return RedirectToAction(nameof(Index));
    }

    // ۴) حذف کاربر / کارمند
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteStaff(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return RedirectToAction(nameof(Index));

        var me = await _users.GetUserAsync(User);
        if (me is not null && user.Id == me.Id)
        {
            TempData["Err"] = "شما نمی‌توانید حساب کاربری فعلی خودتان را حذف کنید.";
            return RedirectToAction(nameof(Index));
        }

        await _users.DeleteAsync(user);
        TempData["Ok"] = $"حساب کاربری «{user.Email}» حذف شد.";
        return RedirectToAction(nameof(Index));
    }

    private async Task EnsureRolesExistAsync()
    {
        foreach (var r in new[] { AppRoles.Admin, AppRoles.Editor, AppRoles.Accountant })
        {
            if (!await _roles.RoleExistsAsync(r))
                await _roles.CreateAsync(new IdentityRole(r));
        }
    }
}
