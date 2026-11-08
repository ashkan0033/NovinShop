using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NovinShop.Models;

namespace NovinShop.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<IdentityUser> _signIn;

    public AccountController(SignInManager<IdentityUser> signIn) => _signIn = signIn;

    [HttpGet("/Account/Login"), HttpGet("/Admin/Account/Login")]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost("/Account/Login"), HttpPost("/Admin/Account/Login"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _signIn.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
            return Url.IsLocalUrl(returnUrl)
                ? LocalRedirect(returnUrl)
                : RedirectToAction("Index", "Dashboard", new { area = "Admin" });

        ModelState.AddModelError("", result.IsLockedOut
            ? "به دلیل تلاش‌های ناموفق، حساب موقتاً قفل شد"
            : "ایمیل یا رمز عبور اشتباه است");
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult Denied()
    {
        TempData["Err"] = "حساب کاربری شما مجوز دسترسی به آن بخش را ندارد (فقط مدیر کل یا نقش مرتبط مجاز است).";
        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
    }
}
