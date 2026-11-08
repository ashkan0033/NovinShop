using System.ComponentModel.DataAnnotations;
using NovinShop.Services;

namespace NovinShop.Models;

public class BlogPost
{
    public int Id { get; set; }

    [Required(ErrorMessage = "عنوان مقاله الزامی است"), MaxLength(160)]
    public string Title { get; set; } = "";

    [MaxLength(160)]
    public string Slug { get; set; } = "";

    [Required(ErrorMessage = "دسته‌بندی مقاله الزامی است"), MaxLength(60)]
    public string Category { get; set; } = "تکنولوژی";

    [MaxLength(350)]
    public string? Summary { get; set; }

    public string? Content { get; set; }

    [MaxLength(200)]
    public string? ImagePath { get; set; }

    [MaxLength(16)]
    public string Icon { get; set; } = "📰";

    [MaxLength(120)]
    public string Gradient { get; set; } = "linear-gradient(135deg,#1e3a8a,#0f172a)";

    [Range(1, 120)]
    public int ReadingMinutes { get; set; } = 5;

    public int ViewCount { get; set; } = 120;

    public bool IsPublished { get; set; } = true;

    public DateTime CreatedAt { get; set; } = Fmt.TehranNow();
}
