using System.ComponentModel.DataAnnotations;

namespace NovinShop.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "ایمیل را وارد کنید")]
    [EmailAddress(ErrorMessage = "ایمیل معتبر نیست")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "رمز عبور را وارد کنید")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public bool RememberMe { get; set; }
}
