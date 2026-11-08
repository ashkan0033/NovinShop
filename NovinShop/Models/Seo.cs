namespace NovinShop.Models;

// وقتی slug محصول/دسته/صفحه عوض شود، آدرس قدیمی با ریدایرکت ۳۰۱ به آدرس جدید می‌رود
public class SlugRedirect
{
    public int Id { get; set; }
    public string Type { get; set; } = "";       // product | category | page
    public string OldSlug { get; set; } = "";
    public string NewSlug { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public record SeoItem(int Id, string Name);
public record SeoReport(List<SeoItem> ProductsNoDesc, List<SeoItem> ProductsNoImage,
                        List<SeoItem> CategoriesNoDesc, List<SeoItem> PagesNoDesc, List<SlugRedirect> Redirects);
