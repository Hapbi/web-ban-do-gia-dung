using Microsoft.EntityFrameworkCore;
using NovaShop.Data;
using NovaShop.Models;

namespace NovaShop.Services;

public class CouponService
{
    private readonly AppDbContext _db;
    public CouponService(AppDbContext db) => _db = db;

    public async Task<(bool ok, Coupon? coupon, string? error)> ValidateAsync(string code, decimal orderTotal)
    {
        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code.ToUpper() == code.Trim().ToUpper());
        if (coupon == null) return (false, null, "Mã giảm giá không tồn tại");
        if (!coupon.IsActive) return (false, null, "Mã giảm giá đã bị vô hiệu hóa");
        if (DateTime.Now < coupon.StartDate) return (false, null, "Mã chưa có hiệu lực");
        if (DateTime.Now > coupon.EndDate) return (false, null, "Mã đã hết hạn");
        if (coupon.UsageLimit > 0 && coupon.UsedCount >= coupon.UsageLimit)
            return (false, null, "Mã đã hết lượt sử dụng");
        if (orderTotal < coupon.MinOrderAmount)
            return (false, null, $"Đơn hàng tối thiểu {coupon.MinOrderAmount:N0}đ");
        return (true, coupon, null);
    }

    public async Task IncrementUsageAsync(string code)
    {
        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code.ToUpper() == code.ToUpper());
        if (coupon != null) { coupon.UsedCount++; await _db.SaveChangesAsync(); }
    }
}


