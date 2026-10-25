using System.ComponentModel.DataAnnotations;

namespace NovaShop.Models;

// Auth
public class LoginVM
{
    [Required(ErrorMessage = "Vui lòng nhập email")]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
    public bool RememberMe { get; set; }
}

public class RegisterVM
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    public string FullName { get; set; } = "";
    [Required, EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    public string Phone { get; set; } = "";
    [Required, MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    public string Password { get; set; } = "";
    [Required, Compare(nameof(Password), ErrorMessage = "Mật khẩu không khớp")]
    public string ConfirmPassword { get; set; } = "";
}

public class ForgotPasswordVM
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";
}

// Product Listing
public class ProductListVM
{
    public List<Product> Products { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<Brand> Brands { get; set; } = new();
    public string? Search { get; set; }
    public string? CategorySlug { get; set; }
    public string? BrandSlug { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MinRating { get; set; }
    public bool InStockOnly { get; set; }
    public string Sort { get; set; } = "newest";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int Total { get; set; }
    public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);
    public string Heading { get; set; } = "Tất cả sản phẩm";
    public List<Product> ComparedProducts { get; set; } = new();
    public List<Product> RecentlyViewed { get; set; } = new();
}

// Product Detail
public class ProductDetailVM
{
    public Product Product { get; set; } = null!;
    public List<Product> Related { get; set; } = new();
    public List<Product> RecentlyViewed { get; set; } = new();
    public bool IsWishlisted { get; set; }
    public bool CanReview { get; set; }
    public bool HasReviewed { get; set; }
    public Review? MyReview { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int[] RatingDistribution { get; set; } = new int[5];
}

// Cart
public class CartLineVM
{
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal LineTotal => Product.CurrentPrice * Quantity;
    public bool HasProblem => Quantity > Product.Stock;
}

public class CartVM
{
    public List<CartLineVM> Lines { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public string? CouponCode { get; set; }
    public string? CouponMessage { get; set; }
    public decimal Shipping { get; set; }
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
    public bool IsEmpty => Lines.Count == 0;
    public bool HasProblems => Lines.Any(l => l.HasProblem);
    public decimal FreeShipThreshold { get; set; } = 5_000_000m;
    public decimal RemainingForFreeShip => Math.Max(0, FreeShipThreshold - (Subtotal - Discount));
    public int FreeShipProgress => FreeShipThreshold <= 0 ? 100
        : (int)Math.Min(100, Math.Round((Subtotal - Discount) / FreeShipThreshold * 100));
    public List<Product> SuggestedProducts { get; set; } = new();
}

// Checkout
public class CheckoutVM
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    public string CustomerName { get; set; } = "";
    [Required, Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string Phone { get; set; } = "";
    [Required, EmailAddress]
    public string Email { get; set; } = "";
    [Required, MinLength(10, ErrorMessage = "Địa chỉ tối thiểu 10 ký tự")]
    public string Address { get; set; } = "";
    public string? Note { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;
    public string? CouponCode { get; set; }
    public bool SaveAddress { get; set; }
    public CartVM Cart { get; set; } = new();
    public List<Address> SavedAddresses { get; set; } = new();
}

// Track Order
public class TrackOrderVM
{
    [Required(ErrorMessage = "Vui lòng nhập mã đơn hàng")]
    public string OrderCode { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    public string Phone { get; set; } = "";
    public Order? Result { get; set; }
    public bool Searched { get; set; }
}

// Review
public class ReviewVM
{
    public int ProductId { get; set; }
    [Required, Range(1, 5)]
    public int Rating { get; set; } = 5;
    [Required, MinLength(5, ErrorMessage = "Tiêu đề tối thiểu 5 ký tự")]
    public string Title { get; set; } = "";
    [Required, MinLength(10, ErrorMessage = "Nội dung tối thiểu 10 ký tự")]
    public string Content { get; set; } = "";
    public string? ImageUrl { get; set; }
}

// Admin Dashboard
public class DashboardVM
{
    public decimal RevenueToday { get; set; }
    public decimal RevenueMonth { get; set; }
    public decimal RevenueTotal { get; set; }
    public decimal RevenueYesterday { get; set; }
    public int PaidOrders { get; set; }
    public int PendingOrders { get; set; }
    public int TotalOrders { get; set; }
    public int TotalUsers { get; set; }
    public int TotalProducts { get; set; }
    public int PendingPayments { get; set; }
    public int PendingListings { get; set; }
    public int NewContacts { get; set; }

    public List<(string Label, decimal Amount, int Count)> Chart7Days { get; set; } = new();
    public List<(string Label, decimal Amount, int Count)> Chart12Months { get; set; } = new();
    public decimal ChartMax { get; set; }

    public int InStock { get; set; }
    public int LowStock { get; set; }
    public int OutStock { get; set; }
    public List<Product> LowStockProducts { get; set; } = new();

    public List<(Product Product, int Qty, decimal Revenue)> TopProducts { get; set; } = new();
    public List<(User User, int OrderCount, decimal Spent)> TopCustomers { get; set; } = new();
    public List<Order> RecentOrders { get; set; } = new();
    public Dictionary<OrderStatus, int> StatusCounts { get; set; } = new();
    public List<Review> RecentReviews { get; set; } = new();
}

// Admin Product Edit
public class ProductEditVM
{
    public int Id { get; set; }
    [Required] public string Name { get; set; } = "";
    public string? Slug { get; set; }
    public string ShortDescription { get; set; } = "";
    public string Description { get; set; } = "";
    [Required] public int CategoryId { get; set; }
    [Required] public int BrandId { get; set; }
    [Required, Range(1, 1000000000)] public decimal OriginalPrice { get; set; }
    public decimal? SalePrice { get; set; }
    [Required, Range(0, 1000000)] public int Stock { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Tags { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public List<(string Name, string Value)> Specs { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<Brand> Brands { get; set; } = new();
}

public class AdminProductListVM
{
    public List<Product> Products { get; set; } = new();
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public bool? IsActive { get; set; }
    public bool LowStockOnly { get; set; }
    public List<Category> Categories { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int Total { get; set; }
    public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);
}

public class AdminOrderListVM
{
    public List<Order> Orders { get; set; } = new();
    public OrderStatus? Status { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int Total { get; set; }
    public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);
    public Dictionary<OrderStatus, int> StatusCounts { get; set; } = new();
}

public class HomeVM
{
    public List<Product> Featured { get; set; } = new();
    public List<Product> NewArrivals { get; set; } = new();
    public List<Product> BestSellers { get; set; } = new();
    public List<Product> FlashSale { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<Brand> Brands { get; set; } = new();
    public List<BlogPost> LatestPosts { get; set; } = new();
    public List<Banner> Banners { get; set; } = new();
    public List<Product> RecentlyViewed { get; set; } = new();
}

public class ProfileVM
{
    public User User { get; set; } = null!;
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public int WishlistCount { get; set; }
    public int ReviewCount { get; set; }
    public List<Order> RecentOrders { get; set; } = new();
}




// Extra admin tools: operations, revenue and shop settings
public class OperationsReportVM
{
    public DateTime Date { get; set; }
    public int OrdersToday { get; set; }
    public int OrdersWeek { get; set; }
    public int PendingPayments { get; set; }
    public int PendingOrders { get; set; }
    public int ShippingOrders { get; set; }
    public int OldPendingOrders { get; set; }
    public int NewContacts { get; set; }
    public int NewListings { get; set; }
    public int LowStockProducts { get; set; }
    public int OutStockProducts { get; set; }
    public List<Order> PendingPaymentOrders { get; set; } = new();
    public List<Order> OldPendingList { get; set; } = new();
    public List<Product> LowStockList { get; set; } = new();
    public List<Product> OutStockList { get; set; } = new();
    public Dictionary<OrderStatus, int> StatusCounts { get; set; } = new();
}

public class RevenueReportVM
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int PaidOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal AvgOrderValue => PaidOrders == 0 ? 0 : Math.Round(TotalRevenue / PaidOrders);
    public decimal PlatformFees { get; set; }
    public List<(DateTime Date, int Orders, decimal Revenue)> Daily { get; set; } = new();
    public List<(string ProductName, int Qty, decimal Revenue)> TopProducts { get; set; } = new();
    public List<(string CategoryName, decimal Revenue)> ByCategory { get; set; } = new();
    public decimal MaxDailyRevenue { get; set; }
}

public class AdminSettingsVM
{
    public SiteSettings Site { get; set; } = new();
    public BankSettings Bank { get; set; } = new();
}


public class SellerListingVM
{
    public List<Product> Listings { get; set; } = new();
    public int PendingCount => Listings.Count(p => p.ListingStatus == ListingStatus.Pending);
    public int ApprovedCount => Listings.Count(p => p.ListingStatus == ListingStatus.Approved);
    public int RejectedCount => Listings.Count(p => p.ListingStatus == ListingStatus.Rejected);
}

public class SellerCreateVM
{
    public int Id { get; set; }
    [Required, MinLength(3, ErrorMessage = "Tên sản phẩm tối thiểu 3 ký tự")]
    public string Name { get; set; } = "";
    [Required(ErrorMessage = "Hãy chọn thương hiệu")]
    public string Brand { get; set; } = "";
    [Required, Range(1, 1000000000, ErrorMessage = "Giá bán phải lớn hơn 0")]
    public decimal Price { get; set; }
    [Required, Range(1, 10000, ErrorMessage = "Số lượng phải từ 1 đến 10.000")]
    public int Stock { get; set; } = 1;
    [Required(ErrorMessage = "Hãy chọn danh mục")]
    public int CategoryId { get; set; }
    [Required, MinLength(20, ErrorMessage = "Mô tả cần ít nhất 20 ký tự")]
    public string Description { get; set; } = "";
    public string? ImageUrl { get; set; }
    public Microsoft.AspNetCore.Http.IFormFile? ImageFile { get; set; }
    public List<Category> Categories { get; set; } = new();
    public List<Brand> Brands { get; set; } = new();
}
