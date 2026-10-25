namespace NovaShop.Models;

// ============================================================
// USER & AUTH
// ============================================================

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Avatar { get; set; } = "";
    public string Role { get; set; } = "Customer"; // Customer | Staff | Admin
    public bool IsActive { get; set; } = true;
    public bool CanVerifyPayments { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastLoginAt { get; set; }

    public List<Address> Addresses { get; set; } = new();
    public List<WishlistItem> Wishlists { get; set; } = new();
}

public class Address
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string ReceiverName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string AddressLine { get; set; } = "";
    public string Ward { get; set; } = "";
    public string District { get; set; } = "";
    public string City { get; set; } = "";
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string FullAddress => $"{AddressLine}, {Ward}, {District}, {City}";
}

// ============================================================
// CATALOG
// ============================================================

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Icon { get; set; } = "📦";
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public List<Product> Products { get; set; } = new();
}

public class Brand
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Logo { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public List<Product> Products { get; set; } = new();
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string ShortDescription { get; set; } = "";
    public string Description { get; set; } = "";
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public int BrandId { get; set; }
    public Brand? Brand { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal? SalePrice { get; set; }
    public int Stock { get; set; }
    public int SoldCount { get; set; }
    public int ViewCount { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    public string Tags { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Marketplace seller listings
    public bool IsMarketplaceListing { get; set; }
    public ListingStatus ListingStatus { get; set; } = ListingStatus.NotListing;
    public int? OwnerUserId { get; set; }
    public User? Owner { get; set; }
    public decimal PlatformFeeRate { get; set; } = 0.07m;
    public string? RejectReason { get; set; }

    public List<ProductImage> Images { get; set; } = new();
    public List<ProductSpec> Specs { get; set; } = new();
    public List<Review> Reviews { get; set; } = new();

    public decimal CurrentPrice => SalePrice is > 0 && SalePrice < OriginalPrice ? SalePrice.Value : OriginalPrice;
    public bool HasDiscount => CurrentPrice < OriginalPrice;
    public int DiscountPercent => HasDiscount ? (int)Math.Round((1 - CurrentPrice / OriginalPrice) * 100) : 0;
    public string PrimaryImage => Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder).FirstOrDefault()?.Url ?? "";
    public double AverageRating => Reviews.Where(r => r.Status == ReviewStatus.Approved).Any()
        ? Reviews.Where(r => r.Status == ReviewStatus.Approved).Average(r => r.Rating) : 0;
    public int ReviewCount => Reviews.Count(r => r.Status == ReviewStatus.Approved);
    public bool InStock => Stock > 0;
    public bool LowStock => Stock > 0 && Stock <= 5;
}

public class ProductImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public string Url { get; set; } = "";
    public string Alt { get; set; } = "";
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

public class ProductSpec
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
    public int SortOrder { get; set; }
}

public class Review
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int Rating { get; set; } // 1-5
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public bool IsVerifiedPurchase { get; set; }
    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

// ============================================================
// CART & ORDERS
// ============================================================

public class CartItem
{
    public int Id { get; set; }
    public string OwnerKey { get; set; } = "";
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.Now;
}

public class Order
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public int UserId { get; set; }
    public User? User { get; set; }
    public string CustomerName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Address { get; set; } = "";
    public string? Note { get; set; }

    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public string? CouponCode { get; set; }
    public decimal Shipping { get; set; }
    public decimal Total { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public PaymentStatus Payment { get; set; } = PaymentStatus.Unpaid;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;
    public string? PaymentProofUrl { get; set; }
    public string? PaymentNote { get; set; }
    public string? TrackingNumber { get; set; }
    public string? CancelReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public List<OrderItem> Items { get; set; } = new();
    public List<OrderStatusHistory> History { get; set; } = new();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public string ProductName { get; set; } = "";
    public string ProductImage { get; set; } = "";
    public string ProductSlug { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public decimal OriginalUnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}

public class OrderStatusHistory
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public OrderStatus Status { get; set; }
    public string Note { get; set; } = "";
    public string ChangedBy { get; set; } = "System";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

// ============================================================
// WISHLIST, COUPON, BLOG, NOTIFICATION
// ============================================================

public class WishlistItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.Now;
}

public class Coupon
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public DiscountType Type { get; set; } = DiscountType.Percentage;
    public decimal Value { get; set; }
    public decimal MinOrderAmount { get; set; }
    public decimal? MaxDiscount { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Now;
    public DateTime EndDate { get; set; } = DateTime.Now.AddDays(30);
    public int UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; } = true;

    public bool IsValid => IsActive && DateTime.Now >= StartDate && DateTime.Now <= EndDate
        && (UsageLimit == 0 || UsedCount < UsageLimit);
}

public class BlogPost
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Content { get; set; } = "";
    public string CoverImage { get; set; } = "";
    public string AuthorName { get; set; } = "";
    public string Category { get; set; } = "";
    public string Tags { get; set; } = "";
    public int ViewCount { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class ContactMessage
{
    public int Id { get; set; }
    public ContactType Type { get; set; } = ContactType.Contact;
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Message { get; set; } = "";
    public ContactStatus Status { get; set; } = ContactStatus.New;
    public string? StaffNote { get; set; }
    public string? HandledBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class Newsletter
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime SubscribedAt { get; set; } = DateTime.Now;
}

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
}

public class Banner
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public string? LinkUrl { get; set; }
    public string Position { get; set; } = "home-top";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Link { get; set; } = "";
    public string Icon { get; set; } = "🔔";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class SiteSettings
{
    public int Id { get; set; }
    public string SiteName { get; set; } = "BTVH";
    public string Tagline { get; set; } = "Điện máy & gia dụng cho mọi nhà";
    public string LogoUrl { get; set; } = "";
    public string Address { get; set; } = "Hà Nội, Việt Nam";
    public string Phone { get; set; } = "0855 081 828";
    public string Email { get; set; } = "hotro@btvh.vn";
    public string OpenHours { get; set; } = "8:00 - 21:00 hằng ngày";
    public string FacebookUrl { get; set; } = "";
    public string YoutubeUrl { get; set; } = "";
    public string ZaloUrl { get; set; } = "";
    public decimal FreeShipThreshold { get; set; } = 5_000_000m;
    public decimal ShippingFee { get; set; } = 30_000m;
    public decimal PlatformFeeRate { get; set; } = 0.07m;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public class BankSettings
{
    public int Id { get; set; }
    public string BankName { get; set; } = "Vietcombank";
    public string AccountNumber { get; set; } = "";
    public string AccountHolder { get; set; } = "";
    public string Branch { get; set; } = "";
    public string QrImageUrl { get; set; } = "";
    public string TransferTemplate { get; set; } = "{OrderCode} - {CustomerName}";
    public string Instructions { get; set; } = "Vui lòng ghi đúng mã đơn hàng trong nội dung chuyển khoản.";
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public class AuditLog
{
    public int Id { get; set; }
    public string UserEmail { get; set; } = "";
    public string Action { get; set; } = "";
    public string Entity { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string Details { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}


