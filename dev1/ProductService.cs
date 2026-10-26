using Microsoft.EntityFrameworkCore;
using NovaShop.Data;
using NovaShop.Models;

namespace NovaShop.Services;

public class ProductService
{
    private readonly AppDbContext _db;
    public ProductService(AppDbContext db) => _db = db;

    public IQueryable<Product> ActiveQuery() =>
        _db.Products.Include(p => p.Images).Include(p => p.Reviews).Include(p => p.Brand).Include(p => p.Category)
            .Where(p => p.IsActive && p.Category != null && p.Category.IsActive && p.Brand != null && p.Brand.IsActive
                && (!p.IsMarketplaceListing || p.ListingStatus == ListingStatus.Approved));

    public async Task<ProductListVM> SearchAsync(
        string? q, string? cat, string? brand, decimal? minPrice, decimal? maxPrice,
        int? minRating, bool inStockOnly, string sort, int page, int pageSize = 12)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 48);
        var query = ActiveQuery();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term)
                || p.Brand!.Name.ToLower().Contains(term)
                || p.Category!.Name.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(cat))
            query = query.Where(p => p.Category!.Slug == cat);
        if (!string.IsNullOrWhiteSpace(brand))
            query = query.Where(p => p.Brand!.Slug == brand);
        if (minPrice.HasValue)
            query = query.Where(p => (double)(p.SalePrice ?? p.OriginalPrice) >= (double)minPrice.Value);
        if (maxPrice.HasValue)
            query = query.Where(p => (double)(p.SalePrice ?? p.OriginalPrice) <= (double)maxPrice.Value);
        if (inStockOnly)
            query = query.Where(p => p.Stock > 0);
        if (minRating.HasValue && minRating.Value > 0)
        {
            var r = minRating.Value;
            query = query.Where(p => p.Reviews.Where(x => x.Status == ReviewStatus.Approved).Average(x => (double?)x.Rating) >= r);
        }

        query = sort switch
        {
            "price-asc" => query.OrderBy(p => (double)(p.SalePrice ?? p.OriginalPrice)),
            "price-desc" => query.OrderByDescending(p => (double)(p.SalePrice ?? p.OriginalPrice)),
            "popular" => query.OrderByDescending(p => p.SoldCount),
            "rating" => query.OrderByDescending(p => p.Reviews.Average(x => (double?)x.Rating) ?? 0),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var total = await query.CountAsync();
        var products = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var categories = await _db.Categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync();
        var brands = await _db.Brands.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();

        var heading = "Tất cả sản phẩm";
        if (!string.IsNullOrWhiteSpace(cat))
        {
            var c = categories.FirstOrDefault(x => x.Slug == cat);
            if (c != null) heading = c.Name;
        }
        else if (!string.IsNullOrWhiteSpace(q)) heading = $"Kết quả cho \"{q}\"";

        return new ProductListVM
        {
            Products = products, Categories = categories, Brands = brands,
            Search = q, CategorySlug = cat, BrandSlug = brand,
            MinPrice = minPrice, MaxPrice = maxPrice, MinRating = minRating,
            InStockOnly = inStockOnly, Sort = sort, Page = page, PageSize = pageSize,
            Total = total, Heading = heading
        };
    }

    public async Task<Product?> GetBySlugAsync(string slug, bool incrementView = false)
    {
        var p = await _db.Products
            .Include(x => x.Images).Include(x => x.Specs).Include(x => x.Category).Include(x => x.Brand)
            .Include(x => x.Reviews).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive
                && x.Category != null && x.Category.IsActive && x.Brand != null && x.Brand.IsActive
                && (!x.IsMarketplaceListing || x.ListingStatus == ListingStatus.Approved));
        if (p == null) return null;

        if (incrementView)
        {
            var tracked = await _db.Products.FindAsync(p.Id);
            if (tracked != null) { tracked.ViewCount++; await _db.SaveChangesAsync(); }
        }
        return p;
    }

    public async Task<List<Product>> GetRelatedAsync(int productId, int categoryId, int count = 4)
    {
        return await ActiveQuery()
            .Where(p => p.CategoryId == categoryId && p.Id != productId)
            .OrderByDescending(p => p.SoldCount)
            .Take(count)
            .ToListAsync();
    }
}


