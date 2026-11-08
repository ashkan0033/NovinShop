namespace NovinShop.Models;

public record CategoryShowcase(Category Category, List<Product> Products, int TotalCount);

public record HomeViewModel(
    List<Category> Categories,
    List<Product> Latest,
    List<Product> Deals,
    List<Banner> Banners,
    List<Product> BestSellers,
    List<CategoryShowcase> CategorySections,
    List<BlogPost>? BlogPosts = null);

public record ShopListViewModel(string Heading, Category? Category, List<Product> Items,
                                string? Q, string Sort, int Page, int TotalPages, int Total);

public record ProductPageViewModel(Product Product, List<Product> Related, List<ProductReview> Reviews)
{
    public double AvgRating => Reviews.Count == 0 ? 0 : Math.Round(Reviews.Average(r => r.Rating), 1);
}

public record CartLine(Product Product, int Qty)
{
    public long Total => (long)Product.Price * Qty;
}

public record CartViewModel(
    List<CartLine> Lines,
    string? CouponCode = null,
    string? CouponTitle = null,
    long Discount = 0,
    string? CouponError = null)
{
    public long Subtotal => Lines.Sum(l => l.Total);
    public long Shipping => Lines.Count == 0 || Subtotal >= 1_000_000 ? 0 : 50_000;
    public long Total => Math.Max(0, Subtotal - Discount) + Shipping;
}

public record DashboardViewModel(int Products, int Active, int Categories, int LowStock, List<Product> Latest);
