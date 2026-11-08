using System.ComponentModel.DataAnnotations;

namespace NovinShop.Models;

public class Category
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام دسته‌بندی الزامی است")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "نام باید بین ۲ تا ۸۰ کاراکتر باشد")]
    public string Name { get; set; } = "";

    [StringLength(120)]
    public string Slug { get; set; } = "";          // آدرس خوانا؛ خالی = خودکار

    [StringLength(500, ErrorMessage = "توضیحات حداکثر ۵۰۰ کاراکتر")]
    public string? Description { get; set; }

    [StringLength(70, ErrorMessage = "عنوان سئو حداکثر ۷۰ کاراکتر")]
    public string? SeoTitle { get; set; }

    [StringLength(170, ErrorMessage = "توضیحات سئو حداکثر ۱۷۰ کاراکتر")]
    public string? SeoDescription { get; set; }

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    [StringLength(8, ErrorMessage = "آیکن باید یک ایموجی کوتاه باشد")]
    public string? Icon { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
