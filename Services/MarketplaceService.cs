using Microsoft.EntityFrameworkCore;
using NovaShop.Data;
using NovaShop.Models;

namespace NovaShop.Services;

public class MarketplaceService
{
    private readonly AppDbContext _db;
    public MarketplaceService(AppDbContext db) => _db = db;

    public async Task<List<Product>> GetPendingAsync() => await _db.Products.AsNoTracking()
        .Include(p => p.Owner).Include(p => p.Category).Include(p => p.Brand).Include(p => p.Images)
        .Where(p => p.IsMarketplaceListing && p.ListingStatus == ListingStatus.Pending).OrderBy(p => p.CreatedAt).ToListAsync();

    public async Task<List<Product>> GetAllListingsAsync() => await _db.Products.AsNoTracking()
        .Include(p => p.Owner).Include(p => p.Category).Include(p => p.Brand).Include(p => p.Images)
        .Where(p => p.IsMarketplaceListing).OrderByDescending(p => p.CreatedAt).ToListAsync();

    public async Task<(bool ok, string msg)> ApproveAsync(int productId)
    {
        var p = await _db.Products.Include(x => x.Category).Include(x => x.Brand)
            .FirstOrDefaultAsync(x => x.Id == productId && x.IsMarketplaceListing);
        if (p == null) return (false, "Không tìm thấy sản phẩm Marketplace");
        if (p.Category == null || !p.Category.IsActive || p.Brand == null || !p.Brand.IsActive)
            return (false, "Không thể duyệt: danh mục hoặc thương hiệu đang bị ẩn. Hãy kích hoạt trước.");
        if (string.IsNullOrWhiteSpace(p.Name) || p.OriginalPrice <= 0 || p.Stock <= 0)
            return (false, "Không thể duyệt sản phẩm thiếu tên, giá hợp lệ hoặc tồn kho.");
        p.ListingStatus = ListingStatus.Approved; p.IsActive = true; p.RejectReason = null; p.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync(); return (true, "Đã duyệt sản phẩm Marketplace");
    }

    public async Task<(bool ok, string msg)> RejectAsync(int productId, string reason)
    {
        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == productId && x.IsMarketplaceListing);
        if (p == null) return (false, "Không tìm thấy sản phẩm Marketplace");
        if (string.IsNullOrWhiteSpace(reason)) return (false, "Hãy ghi lý do từ chối để người đăng biết cần sửa gì");
        p.ListingStatus = ListingStatus.Rejected; p.IsActive = false; p.RejectReason = reason.Trim(); p.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync(); return (true, "Đã từ chối và lưu lý do");
    }
}
