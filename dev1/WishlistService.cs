using Microsoft.EntityFrameworkCore;
using NovaShop.Data;

namespace NovaShop.Services;

public class WishlistService
{
    private readonly AppDbContext _db;
    private readonly AuthService _auth;
    public WishlistService(AppDbContext db, AuthService auth) { _db = db; _auth = auth; }

    public async Task<bool> IsWishlistedAsync(int productId)
    {
        var uid = _auth.GetCurrentId();
        if (uid == null) return false;
        return await _db.Wishlists.AnyAsync(w => w.UserId == uid.Value && w.ProductId == productId);
    }

    public async Task<(bool ok, string msg)> ToggleAsync(int productId)
    {
        var uid = _auth.GetCurrentId();
        if (uid == null) return (false, "Vui lòng đăng nhập");

        var existing = await _db.Wishlists.FirstOrDefaultAsync(w => w.UserId == uid.Value && w.ProductId == productId);
        if (existing != null)
        {
            _db.Wishlists.Remove(existing);
            await _db.SaveChangesAsync();
            return (true, "Đã xóa khỏi yêu thích");
        }
        var publicProduct = await _db.Products.AnyAsync(p => p.Id == productId && p.IsActive
            && p.Category != null && p.Category.IsActive && p.Brand != null && p.Brand.IsActive
            && (!p.IsMarketplaceListing || p.ListingStatus == Models.ListingStatus.Approved));
        if (!publicProduct) return (false, "Sản phẩm không còn được hiển thị công khai");
        _db.Wishlists.Add(new Models.WishlistItem { UserId = uid.Value, ProductId = productId });
        await _db.SaveChangesAsync();
        return (true, "Đã thêm vào yêu thích");
    }

    public async Task<List<Models.Product>> GetUserWishlistAsync()
    {
        var uid = _auth.GetCurrentId();
        if (uid == null) return new();
        return await _db.Wishlists
            .Where(w => w.UserId == uid.Value && w.Product != null && w.Product.IsActive
                && w.Product.Category != null && w.Product.Category.IsActive && w.Product.Brand != null && w.Product.Brand.IsActive
                && (!w.Product.IsMarketplaceListing || w.Product.ListingStatus == Models.ListingStatus.Approved))
            .Include(w => w.Product).ThenInclude(p => p!.Images)
            .Include(w => w.Product).ThenInclude(p => p!.Reviews)
            .Include(w => w.Product).ThenInclude(p => p!.Brand)
            .Include(w => w.Product).ThenInclude(p => p!.Category)
            .OrderByDescending(w => w.AddedAt)
            .Select(w => w.Product!)
            .ToListAsync();
    }
}


