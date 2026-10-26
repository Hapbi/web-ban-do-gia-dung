using Microsoft.EntityFrameworkCore;
using NovaShop.Data;
using NovaShop.Models;

namespace NovaShop.Services;

public class ReviewService
{
    private readonly AppDbContext _db;
    private readonly AuthService _auth;
    public ReviewService(AppDbContext db, AuthService auth) { _db = db; _auth = auth; }

    public async Task<(bool ok, string msg)> CreateAsync(ReviewVM vm)
    {
        var uid = _auth.GetCurrentId();
        if (uid == null) return (false, "Vui lòng đăng nhập");

        // Check if user has purchased this product (verified purchase)
        var hasPurchased = await _db.Orders
            .Where(o => o.UserId == uid.Value && o.Status == OrderStatus.Delivered)
            .SelectMany(o => o.Items)
            .AnyAsync(i => i.ProductId == vm.ProductId);

        if (!hasPurchased) return (false, "Chỉ có thể đánh giá sản phẩm sau khi đơn hàng được giao thành công");

        var alreadyReviewed = await _db.Reviews.AnyAsync(r => r.ProductId == vm.ProductId && r.UserId == uid.Value);
        if (alreadyReviewed) return (false, "Bạn đã đánh giá sản phẩm này rồi");

        _db.Reviews.Add(new Review
        {
            ProductId = vm.ProductId,
            UserId = uid.Value,
            Rating = Math.Clamp(vm.Rating, 1, 5),
            Title = vm.Title.Trim(),
            Content = vm.Content.Trim(),
            ImageUrl = vm.ImageUrl ?? "",
            IsVerifiedPurchase = hasPurchased,
            Status = ReviewStatus.Pending // Admin reviews the submission before it appears publicly
        });
        await _db.SaveChangesAsync();
        return (true, "Cảm ơn bạn đã đánh giá!");
    }

    public async Task<(bool ok, string msg)> ApproveAsync(int reviewId)
    {
        var r = await _db.Reviews.FindAsync(reviewId);
        if (r == null) return (false, "Không tìm thấy");
        r.Status = ReviewStatus.Approved;
        await _db.SaveChangesAsync();
        return (true, "Đã duyệt");
    }

    public async Task<(bool ok, string msg)> RejectAsync(int reviewId)
    {
        var r = await _db.Reviews.FindAsync(reviewId);
        if (r == null) return (false, "Không tìm thấy");
        r.Status = ReviewStatus.Rejected;
        await _db.SaveChangesAsync();
        return (true, "Đã từ chối");
    }
}


