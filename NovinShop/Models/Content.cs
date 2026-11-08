using System.ComponentModel.DataAnnotations;

namespace NovinShop.Models;

// تنظیمات و متن‌های سایت: کلید/مقدار (کلیدهای مجاز در SiteSettings.Defs تعریف شده‌اند)
public class SiteSetting
{
    [Key, MaxLength(100)] public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

// صفحه‌ی ثابت (درباره ما، قوانین، ...)
public class ContentPage
{
    public int Id { get; set; }

    [Required(ErrorMessage = "عنوان صفحه الزامی است")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "عنوان باید بین ۲ تا ۱۲۰ کاراکتر باشد")]
    public string Title { get; set; } = "";

    [StringLength(160)] public string Slug { get; set; } = "";
    public string? Content { get; set; }
    public bool IsActive { get; set; } = true;
    public bool ShowInFooter { get; set; } = true;
    public int SortOrder { get; set; }

    [StringLength(70, ErrorMessage = "عنوان سئو حداکثر ۷۰ کاراکتر")] public string? SeoTitle { get; set; }
    [StringLength(170, ErrorMessage = "توضیحات سئو حداکثر ۱۷۰ کاراکتر")] public string? SeoDescription { get; set; }
}

// اسلایدر صفحه‌ی اصلی
public class Banner
{
    public int Id { get; set; }

    [Required(ErrorMessage = "عنوان بنر الزامی است")]
    [StringLength(100, ErrorMessage = "عنوان حداکثر ۱۰۰ کاراکتر")]
    public string Title { get; set; } = "";

    [StringLength(250, ErrorMessage = "زیرعنوان حداکثر ۲۵۰ کاراکتر")] public string? Subtitle { get; set; }
    [StringLength(40, ErrorMessage = "متن دکمه حداکثر ۴۰ کاراکتر")] public string? ButtonText { get; set; }

    [StringLength(300)]
    [RegularExpression(@"^(/|https?://).*", ErrorMessage = "آدرس باید با / یا http:// یا https:// شروع شود")]
    public string? LinkUrl { get; set; }

    public string? ImagePath { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
