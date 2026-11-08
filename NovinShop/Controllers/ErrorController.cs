using Microsoft.AspNetCore.Mvc;

namespace NovinShop.Controllers;

public class ErrorController : Controller
{
    // UseStatusCodePagesWithReExecute همه‌ی 404/403/500 های خالی را اینجا می‌آورد
    [Route("error/{code:int}")]
    public IActionResult Index(int code)
    {
        Response.StatusCode = code;               // برای سئو باید کد واقعی (مثلاً 404) برگردد، نه 200
        ViewData["Robots"] = "noindex,nofollow";
        ViewData["Title"] = code == 404 ? "صفحه پیدا نشد | نوین‌شاپ" : "خطا | نوین‌شاپ";
        return View(code);
    }
}
