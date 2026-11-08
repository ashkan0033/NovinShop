using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NovinShop.Models;

namespace NovinShop.Data;

public class AppDbContext : IdentityDbContext<IdentityUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<ContentPage> ContentPages => Set<ContentPage>();
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<SlugRedirect> SlugRedirects => Set<SlugRedirect>();
    public DbSet<PaymentGateway> PaymentGateways => Set<PaymentGateway>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<AccountingEntry> AccountingEntries => Set<AccountingEntry>();
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Category>().HasIndex(c => c.Slug).IsUnique();
        b.Entity<Product>().HasIndex(p => p.Slug).IsUnique();
        b.Entity<Product>()
            .HasOne(p => p.Category).WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Order>().HasIndex(o => o.Code).IsUnique();
        b.Entity<Order>().HasIndex(o => o.Phone);
        b.Entity<ContentPage>().HasIndex(p => p.Slug).IsUnique();
        b.Entity<SlugRedirect>().HasIndex(r => new { r.Type, r.OldSlug });
        b.Entity<PaymentGateway>().HasIndex(g => g.Key).IsUnique();
        b.Entity<Coupon>().HasIndex(c => c.Code).IsUnique();
        b.Entity<ProductReview>().HasIndex(r => new { r.ProductId, r.IsApproved });
        b.Entity<ProductReview>()
            .HasOne(r => r.Product).WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<InventoryTransaction>().HasIndex(t => t.ProductId);
        b.Entity<InventoryTransaction>().HasIndex(t => t.CreatedAt);
        b.Entity<InventoryTransaction>()
            .HasOne(t => t.Product).WithMany()
            .HasForeignKey(t => t.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<AccountingEntry>().HasIndex(a => a.CreatedAt);
        b.Entity<BlogPost>().HasIndex(bp => bp.Slug).IsUnique();
        b.Entity<Order>()
            .HasMany(o => o.Items).WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
