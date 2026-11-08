using System.ComponentModel.DataAnnotations;

namespace NovinShop.Models;

public class ProductReview
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [Required(ErrorMessage = "نام خود را وارد کنید")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "نام باید بین ۲ تا ۸۰ کاراکتر باشد")]
    public string AuthorName { get; set; } = "";

    [Range(1, 5, ErrorMessage = "امتیاز باید بین ۱ تا ۵ ستاره باشد")]
    public int Rating { get; set; } = 5;

    [StringLength(120)]
    public string? Title { get; set; }

    [Required(ErrorMessage = "متن دیدگاه را وارد کنید")]
    [StringLength(1500, MinimumLength = 5, ErrorMessage = "متن دیدگاه باید حداقل ۵ کاراکتر باشد")]
    public string Comment { get; set; } = "";

    [StringLength(250)]
    public string? Pros { get; set; }

    [StringLength(250)]
    public string? Cons { get; set; }

    public bool IsBuyer { get; set; }

    public bool IsApproved { get; set; }

    [StringLength(600)]
    public string? AdminReply { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
