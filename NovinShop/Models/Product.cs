using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace NovinShop.Models;

public record ProductSpec(string Key, string Value);

public class Product
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام محصول الزامی است")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "نام باید بین ۳ تا ۱۵۰ کاراکتر باشد")]
    public string Name { get; set; } = "";

    [StringLength(160)]
    public string Slug { get; set; } = "";

    [StringLength(300, ErrorMessage = "توضیح کوتاه حداکثر ۳۰۰ کاراکتر")]
    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "قیمت معتبر نیست")]
    public int Price { get; set; }                  // قیمت فروش (تومان)

    public int CostPrice { get; set; }              // آخرین قیمت خرید واحد در انبار (تومان)

    public int? OldPrice { get; set; }              // قیمت قبل از تخفیف

    [Range(0, 100000, ErrorMessage = "موجودی معتبر نیست")]
    public int Stock { get; set; }

    public string? ImagePath { get; set; }
    public string? GalleryJson { get; set; }        // آرایه JSON از تصاویر گالری
    public string? SpecsJson { get; set; }          // آرایه JSON از مشخصات فنی (Key/Value)
    public bool IsActive { get; set; } = true;

    [Range(1, int.MaxValue, ErrorMessage = "دسته‌بندی را انتخاب کنید")]
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(70, ErrorMessage = "عنوان سئو حداکثر ۷۰ کاراکتر")]
    public string? SeoTitle { get; set; }

    [StringLength(170, ErrorMessage = "توضیحات سئو حداکثر ۱۷۰ کاراکتر")]
    public string? SeoDescription { get; set; }

    public List<string> GetGallery()
    {
        if (string.IsNullOrWhiteSpace(GalleryJson)) return new();
        try { return JsonSerializer.Deserialize<List<string>>(GalleryJson) ?? new(); }
        catch { return new(); }
    }

    public List<string> GetAllImages()
    {
        var list = new List<string>();
        if (!string.IsNullOrWhiteSpace(ImagePath)) list.Add(ImagePath);
        foreach (var img in GetGallery())
        {
            if (!string.IsNullOrWhiteSpace(img) && !list.Contains(img))
                list.Add(img);
        }
        return list;
    }

    public List<ProductSpec> GetSpecs()
    {
        if (string.IsNullOrWhiteSpace(SpecsJson)) return new();
        try { return JsonSerializer.Deserialize<List<ProductSpec>>(SpecsJson) ?? new(); }
        catch { return new(); }
    }
}

public record ProductListViewModel(List<Product> Items, string? Q, int? CategoryId, int Page, int TotalPages, int Total);
