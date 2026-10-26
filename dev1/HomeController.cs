using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovaShop.Data;
using NovaShop.Models;
using NovaShop.Services;

namespace NovaShop.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly RecentlyViewedService _recent;

    public HomeController(AppDbContext db, RecentlyViewedService recent) { _db = db; _recent = recent; }

    public async Task<IActionResult> Index()
    {
        var query = _db.Products.Include(p => p.Images).Include(p => p.Reviews).Include(p => p.Brand).Include(p => p.Category)
            .Where(p => p.IsActive && p.Category != null && p.Category.IsActive && p.Brand != null && p.Brand.IsActive
                && (!p.IsMarketplaceListing || p.ListingStatus == ListingStatus.Approved));
        var vm = new HomeVM
        {
            Featured = await query.Where(p => p.IsFeatured).OrderByDescending(p => p.SoldCount).Take(8).ToListAsync(),
            NewArrivals = await query.OrderByDescending(p => p.CreatedAt).Take(8).ToListAsync(),
            BestSellers = await query.OrderByDescending(p => p.SoldCount).Take(8).ToListAsync(),
            FlashSale = await query.Where(p => p.SalePrice != null && p.SalePrice < p.OriginalPrice).OrderByDescending(p => ((double)p.OriginalPrice - (double)(p.SalePrice ?? 0m)) / (double)p.OriginalPrice).Take(6).ToListAsync(),
            Categories = await _db.Categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync(),
            Brands = await _db.Brands.Where(b => b.IsActive).ToListAsync(),
            LatestPosts = await _db.BlogPosts.Where(b => b.IsPublished).OrderByDescending(b => b.PublishedAt).Take(3).ToListAsync(),
            Banners = await _db.Banners.Where(b => b.IsActive && b.Position == "home-top").OrderBy(b => b.SortOrder).Take(3).ToListAsync(),
            RecentlyViewed = await _recent.GetAsync()
        };
        return View(vm);
    }

    [Route("loi")]
    public IActionResult Error() => View();

    [Route("khong-tim-thay")]
    public IActionResult NotFound404() { Response.StatusCode = 404; return View(); }
}


