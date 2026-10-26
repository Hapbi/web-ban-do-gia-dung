using Microsoft.EntityFrameworkCore;
using NovaShop.Data;
using NovaShop.Models;

namespace NovaShop.Services;

public class CartService
{
    public const decimal FreeShipThreshold = 5_000_000m;
    public const decimal ShippingFee = 30_000m;
    private const int MaxPerLine = 20;

    private readonly AppDbContext _db;
    private readonly AuthService _auth;
    private readonly CouponService _coupons;
    private readonly SettingsService _settings;

    public CartService(AppDbContext db, AuthService auth, CouponService coupons, SettingsService settings)
    { _db = db; _auth = auth; _coupons = coupons; _settings = settings; }

    public async Task<CartVM> GetAsync(string? couponCode = null)
    {
        var key = _auth.GetOwnerKey();
        var items = await _db.CartItems
            .Include(c => c.Product).ThenInclude(p => p!.Images)
            .Include(c => c.Product).ThenInclude(p => p!.Brand)
            .Include(c => c.Product).ThenInclude(p => p!.Category)
            .Where(c => c.OwnerKey == key && c.Product != null && c.Product.IsActive
                && c.Product.Category != null && c.Product.Category.IsActive && c.Product.Brand != null && c.Product.Brand.IsActive
                && (!c.Product.IsMarketplaceListing || c.Product.ListingStatus == ListingStatus.Approved))
            .ToListAsync();

        var shop = await _settings.GetSiteAsync();
        var freeShipThreshold = Math.Max(0, shop.FreeShipThreshold);
        var shippingFee = Math.Max(0, shop.ShippingFee);
        var vm = new CartVM { FreeShipThreshold = freeShipThreshold };
        foreach (var c in items)
            vm.Lines.Add(new CartLineVM { Product = c.Product!, Quantity = c.Quantity });

        vm.Subtotal = vm.Lines.Sum(l => l.Product.OriginalPrice * l.Quantity);
        var merchandise = vm.Lines.Sum(l => l.Product.CurrentPrice * l.Quantity);

        // Apply coupon
        decimal couponDiscount = 0;
        var freeShippingCoupon = false;
        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            var (ok, coupon, err) = await _coupons.ValidateAsync(couponCode, merchandise);
            if (ok && coupon != null)
            {
                freeShippingCoupon = string.Equals(coupon.Code, "FREESHIP", StringComparison.OrdinalIgnoreCase);
                if (!freeShippingCoupon)
                {
                    couponDiscount = coupon.Type == DiscountType.Percentage
                        ? Math.Min(merchandise * coupon.Value / 100, coupon.MaxDiscount ?? decimal.MaxValue)
                        : coupon.Value;
                    couponDiscount = Math.Min(couponDiscount, merchandise);
                }
                vm.CouponCode = coupon.Code;
                vm.CouponMessage = freeShippingCoupon ? "Đã áp dụng: miễn phí vận chuyển." : $"Đã áp dụng mã {coupon.Code}.";
            }
            else vm.CouponMessage = err ?? "Không thể áp dụng mã giảm giá.";
        }

        vm.Discount = (vm.Subtotal - merchandise) + couponDiscount;
        var afterDiscount = merchandise - couponDiscount;
        vm.Shipping = vm.Lines.Count == 0 || freeShippingCoupon ? 0 : (afterDiscount >= freeShipThreshold ? 0 : shippingFee);
        vm.Total = Math.Max(0, afterDiscount + vm.Shipping);
        vm.ItemCount = vm.Lines.Sum(l => l.Quantity);

        // Suggested products (same categories as items)
        if (vm.Lines.Any())
        {
            var catIds = vm.Lines.Select(l => l.Product.CategoryId).Distinct().ToList();
            var inCart = vm.Lines.Select(l => l.Product.Id).ToList();
            vm.SuggestedProducts = await _db.Products
                .Include(p => p.Images).Include(p => p.Reviews).Include(p => p.Brand).Include(p => p.Category)
                .Where(p => p.IsActive && p.Category != null && p.Category.IsActive && p.Brand != null && p.Brand.IsActive
                    && (!p.IsMarketplaceListing || p.ListingStatus == ListingStatus.Approved)
                    && catIds.Contains(p.CategoryId) && !inCart.Contains(p.Id))
                .OrderByDescending(p => p.SoldCount).Take(4).ToListAsync();
        }

        return vm;
    }

    public async Task<(bool ok, string msg)> AddAsync(int productId, int quantity = 1)
    {
        var key = _auth.GetOwnerKey();
        if (quantity <= 0) return (false, "Số lượng cần lớn hơn 0");
        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == productId && x.IsActive
            && x.Category != null && x.Category.IsActive && x.Brand != null && x.Brand.IsActive
            && (!x.IsMarketplaceListing || x.ListingStatus == ListingStatus.Approved));
        if (p == null) return (false, "Sản phẩm không tồn tại");
        if (p.Stock <= 0) return (false, "Sản phẩm đã hết hàng");

        var existing = await _db.CartItems.FirstOrDefaultAsync(c => c.OwnerKey == key && c.ProductId == productId);
        var current = existing?.Quantity ?? 0;
        var limit = Math.Min(p.Stock, MaxPerLine);
        if (current >= limit || quantity > limit - current)
            return (false, $"Chỉ có thể mua tối đa {limit} sản phẩm");

        if (existing != null) existing.Quantity += quantity;
        else _db.CartItems.Add(new CartItem { OwnerKey = key, ProductId = productId, Quantity = quantity });
        await _db.SaveChangesAsync();
        return (true, "Đã thêm vào giỏ hàng");
    }

    public async Task UpdateAsync(int productId, int quantity)
    {
        var key = _auth.GetOwnerKey();
        var existing = await _db.CartItems.FirstOrDefaultAsync(c => c.OwnerKey == key && c.ProductId == productId);
        if (existing == null) return;
        if (quantity <= 0) _db.CartItems.Remove(existing);
        else
        {
            var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == productId && x.IsActive
                && x.Category != null && x.Category.IsActive && x.Brand != null && x.Brand.IsActive
                && (!x.IsMarketplaceListing || x.ListingStatus == ListingStatus.Approved));
            if (p == null || p.Stock <= 0) _db.CartItems.Remove(existing);
            else existing.Quantity = Math.Min(quantity, Math.Min(p.Stock, MaxPerLine));
            if (existing.Quantity <= 0) _db.CartItems.Remove(existing);
        }
        await _db.SaveChangesAsync();
    }

    public async Task RemoveAsync(int productId)
    {
        var key = _auth.GetOwnerKey();
        var items = await _db.CartItems.Where(c => c.OwnerKey == key && c.ProductId == productId).ToListAsync();
        _db.CartItems.RemoveRange(items);
        await _db.SaveChangesAsync();
    }

    public async Task ClearAsync()
    {
        var key = _auth.GetOwnerKey();
        var items = await _db.CartItems.Where(c => c.OwnerKey == key).ToListAsync();
        _db.CartItems.RemoveRange(items);
        await _db.SaveChangesAsync();
    }

    public async Task<int> GetCountAsync()
    {
        var key = _auth.GetOwnerKey();
        return await _db.CartItems.Where(c => c.OwnerKey == key && c.Product != null && c.Product.IsActive)
            .SumAsync(c => (int?)c.Quantity) ?? 0;
    }
}


