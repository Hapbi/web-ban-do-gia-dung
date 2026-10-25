using Microsoft.EntityFrameworkCore;
using NovaShop.Models;

namespace NovaShop.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> o) : base(o) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductSpec> ProductSpecs => Set<ProductSpec>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderHistories => Set<OrderStatusHistory>();
    public DbSet<WishlistItem> Wishlists => Set<WishlistItem>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<Newsletter> Newsletters => Set<Newsletter>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();
    public DbSet<BankSettings> BankSettings => Set<BankSettings>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Unique indexes
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();
        b.Entity<Product>().HasIndex(p => p.Slug).IsUnique();
        b.Entity<Category>().HasIndex(c => c.Slug).IsUnique();
        b.Entity<Brand>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<Order>().HasIndex(o => o.Code).IsUnique();
        b.Entity<Coupon>().HasIndex(c => c.Code).IsUnique();
        b.Entity<BlogPost>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<Newsletter>().HasIndex(n => n.Email).IsUnique();

        // Prevent cascade delete cycle
        b.Entity<Order>().HasOne(o => o.User).WithMany().HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<OrderItem>().HasOne<Order>().WithMany(o => o.Items).HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<OrderItem>().HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<OrderStatusHistory>().HasOne<Order>().WithMany(o => o.History).HasForeignKey(h => h.OrderId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<Product>().HasOne(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Product>().HasOne(p => p.Brand).WithMany(x => x.Products).HasForeignKey(p => p.BrandId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Product>().HasOne(p => p.Owner).WithMany().HasForeignKey(p => p.OwnerUserId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<ProductImage>().HasOne<Product>().WithMany(p => p.Images).HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ProductSpec>().HasOne<Product>().WithMany(p => p.Specs).HasForeignKey(s => s.ProductId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Review>().HasOne(p => p.Product).WithMany(p => p.Reviews).HasForeignKey(r => r.ProductId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Review>().HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<WishlistItem>().HasIndex(w => new { w.UserId, w.ProductId }).IsUnique();
        b.Entity<Address>().HasOne<User>().WithMany(u => u.Addresses).HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}


