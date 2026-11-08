using System.ComponentModel.DataAnnotations;
using NovinShop.Services;

namespace NovinShop.Models;

public class Coupon
{
    public int Id { get; set; }

    [Required(ErrorMessage = "کد تخفیف را وارد کنید")]
    [StringLength(40, MinimumLength = 3, ErrorMessage = "کد تخفیف باید بین ۳ تا ۴۰ کاراکتر باشد")]
    public string Code { get; set; } = "";

    [Required(ErrorMessage = "عنوان کد تخفیف را وارد کنید")]
    [StringLength(120)]
    public string Title { get; set; } = "";

    /// <summary>
    /// true = تخفیف درصدی (مثلاً ۱۵٪) | false = مبلغ ثابت به تومان (مثلاً ۱۰۰,۰۰۰ تومان)
    /// </summary>
    public bool IsPercent { get; set; } = true;

    [Range(1, 100_000_000, ErrorMessage = "مقدار تخفیف نامعتبر است")]
    public long Value { get; set; } = 10;

    /// <summary>
    /// سقف مبلغ تخفیف (تومان) برای کوپن‌های درصدی (اختیاری)
    /// </summary>
    public long? MaxDiscount { get; set; }

    /// <summary>
    /// حداقل مبلغ سبد خرید (تومان) برای مجاز بودن کد تخفیف
    /// </summary>
    public long MinOrderAmount { get; set; }

    /// <summary>
    /// سقف تعداد دفعات مجاز استفاده (null = نامحدود)
    /// </summary>
    public int? UsageLimit { get; set; }

    public int UsedCount { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public (bool Ok, long Discount, string? Error) CalculateDiscount(long subtotal, DateTime nowUtc)
    {
        if (!IsActive)
            return (false, 0, "این کد تخفیف غیرفعال شده است.");

        if (ExpiresAt is not null && ExpiresAt.Value < nowUtc)
            return (false, 0, "مهلت استفاده از این کد تخفیف به پایان رسیده است.");

        if (UsageLimit is not null && UsedCount >= UsageLimit.Value)
            return (false, 0, "ظرفیت استفاده از این کد تخفیف تکمیل شده است.");

        if (subtotal <= 0)
            return (false, 0, "سبد خرید شما خالی است.");

        if (MinOrderAmount > 0 && subtotal < MinOrderAmount)
            return (false, 0, $"حداقل مبلغ سفارش برای استفاده از کد «{Code}»، {Fmt.Toman(MinOrderAmount)} است.");

        long discount;
        if (IsPercent)
        {
            var pct = Math.Clamp(Value, 1, 100);
            discount = (long)Math.Round(subtotal * (pct / 100.0));
            if (MaxDiscount is > 0 && discount > MaxDiscount.Value)
                discount = MaxDiscount.Value;
        }
        else
        {
            discount = Value;
        }

        discount = Math.Clamp(discount, 0, subtotal);
        return (true, discount, null);
    }
}
