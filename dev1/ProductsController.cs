using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovaShop.Data;
using NovaShop.Models;
using NovaShop.Services;

namespace NovaShop.Controllers;

public class ProductsController : Controller
{
    private readonly AppDbContext _db;
    private readonly ProductService _products;
    private readonly WishlistService _wishlist;
    private readonly ReviewService _reviews;
    private readonly RecentlyViewedService _recent;
    private readonly AuthService _auth;

    public ProductsController(AppDbContext db, ProductService products, WishlistService wishlist, ReviewService reviews, RecentlyViewedService recent, AuthService auth)
    { _db = db; _products = products; _wishlist = wishlist; _reviews = reviews; _recent = recent; _auth = auth; }

    [Route("san-pham")]
    public async Task<IActionResult> Index(
        string? q, string? cat, string? brand, decimal? minPrice, decimal? maxPrice,
        int? minRating, bool inStockOnly = false, string sort = "newest", int page = 1)
    {
        var vm = await _products.SearchAsync(q, cat, brand, minPrice, maxPrice, minRating, inStockOnly, sort, page);
        vm.RecentlyViewed = await _recent.GetAsync();
        return View(vm);
    }

    [Route("san-pham/{slug}")]
    public async Task<IActionResult> Detail(string slug)
    {
        var product = await _products.GetBySlugAsync(slug, incrementView: true);
        if (product == null) return NotFound();

        var uid = _auth.GetCurrentId();
        var myReview = uid.HasValue ? product.Reviews.FirstOrDefault(r => r.UserId == uid.Value) : null;

        // Can review: has purchased, delivered, not reviewed
        bool canReview = false;
        if (uid.HasValue && myReview == null)
        {
            canReview = await _db.Orders
                .Where(o => o.UserId == uid.Value && o.Status == OrderStatus.Delivered)
                .SelectMany(o => o.Items)
                .AnyAsync(i => i.ProductId == product.Id);
        }

        _recent.Record(product.Id);

        var ratings = product.Reviews.Where(r => r.Status == ReviewStatus.Approved).ToList();
        var vm = new ProductDetailVM
        {
            Product = product,
            Related = await _products.GetRelatedAsync(product.Id, product.CategoryId),
            RecentlyViewed = await _recent.GetAsync(product.Id),
            IsWishlisted = await _wishlist.IsWishlistedAsync(product.Id),
            CanReview = canReview,
            HasReviewed = myReview != null,
            MyReview = myReview,
            AverageRating = ratings.Any() ? ratings.Average(r => r.Rating) : 0,
            ReviewCount = ratings.Count,
            RatingDistribution = new int[5]
        };

        for (int i = 1; i <= 5; i++) vm.RatingDistribution[i - 1] = ratings.Count(r => r.Rating == i);

        return View(vm);
    }

    [HttpPost]
    [Route("danh-gia")]
    public async Task<IActionResult> AddReview(ReviewVM vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Vui lòng kiểm tra lại thông tin";
            return RedirectToAction("Detail", new { slug = (await _db.Products.FindAsync(vm.ProductId))?.Slug });
        }
        var (ok, msg) = await _reviews.CreateAsync(vm);
        TempData[ok ? "Success" : "Error"] = msg;
        var slug = (await _db.Products.FindAsync(vm.ProductId))?.Slug;
        return RedirectToAction("Detail", new { slug });
    }
}


