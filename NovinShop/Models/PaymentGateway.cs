using System.ComponentModel.DataAnnotations;

namespace NovinShop.Models;

// ماژول درگاه / روش پرداخت قابل مدیریت از پنل
public class PaymentGateway
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Key { get; set; } = "";            // zarinpal | card_to_card | simulator

    [Required, MaxLength(100)]
    public string Title { get; set; } = "";          // عنوان نمایشی به مشتری

    [MaxLength(250)]
    public string? Description { get; set; }         // توضیح کوتاه زیر عنوان در صفحه‌ی پرداخت

    public bool IsEnabled { get; set; } = true;
    public bool IsSandbox { get; set; } = true;      // حالت آزمایشی (Sandbox) برای درگاه‌های آنلاین
    public int SortOrder { get; set; }

    // تنظیمات اختصاصی زرین‌پال و درگاه‌های آنلاین
    [MaxLength(100)]
    public string? MerchantId { get; set; }          // کد درگاه ۳۶ کاراکتری زرین‌پال

    // تنظیمات اختصاصی کارت به کارت
    [MaxLength(30)]
    public string? CardNumber { get; set; }          // شماره کارت ۱۶ رقمی

    [MaxLength(100)]
    public string? CardHolder { get; set; }          // نام صاحب حساب

    [MaxLength(80)]
    public string? BankName { get; set; }            // نام بانک

    [MaxLength(40)]
    public string? ShebaNumber { get; set; }         // شماره شبا (اختیاری)
}
