using Microsoft.EntityFrameworkCore;
using NovaShop.Data;
using NovaShop.Models;

namespace NovaShop.Services;

public class OrderService
{
    private readonly AppDbContext _db;
    private readonly CartService _cart;
    private readonly AuthService _auth;
    private readonly CouponService _coupons;
    private readonly NotificationService _notif;

    public OrderService(AppDbContext db, CartService cart, AuthService auth, CouponService coupons, NotificationService notif)
    { _db = db; _cart = cart; _auth = auth; _coupons = coupons; _notif = notif; }

    public async Task<(bool ok, string? code, string? err)> PlaceOrderAsync(CheckoutVM input)
    {
        var user = await _auth.GetCurrentAsync();
        if (user == null) return (false, null, "Vui lòng đăng nhập");

        var cart = await _cart.GetAsync(input.CouponCode);
        if (cart.IsEmpty) return (false, null, "Giỏ hàng trống");
        if (cart.HasProblems) return (false, null, "Một số sản phẩm vượt quá tồn kho");

        foreach (var line in cart.Lines)
        {
            var fresh = await _db.Products.FindAsync(line.Product.Id);
            if (fresh == null || fresh.Stock < line.Quantity)
                return (false, null, $"'{line.Product.Name}' chỉ còn {fresh?.Stock ?? 0} sản phẩm");
        }

        string code;
        do { code = "DH" + DateTime.Now.ToString("yyMMdd") + Guid.NewGuid().ToString("N")[..4].ToUpper(); }
        while (await _db.Orders.AnyAsync(o => o.Code == code));

        var order = new Order
        {
            Code = code,
            UserId = user.Id,
            CustomerName = input.CustomerName.Trim(),
            Phone = input.Phone.Trim(),
            Email = input.Email.Trim(),
            Address = input.Address.Trim(),
            Note = input.Note?.Trim(),
            Subtotal = cart.Subtotal,
            Discount = cart.Discount,
            CouponCode = cart.CouponCode,
            Shipping = cart.Shipping,
            Total = cart.Total,
            Status = OrderStatus.Pending,
            Payment = PaymentStatus.Unpaid,
            PaymentMethod = input.PaymentMethod,
            CreatedAt = DateTime.Now
        };
        order.History.Add(new OrderStatusHistory { Status = OrderStatus.Pending, Note = "Đơn hàng được tạo", ChangedBy = "Customer" });

        foreach (var l in cart.Lines)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = l.Product.Id,
                ProductName = l.Product.Name,
                ProductImage = l.Product.PrimaryImage,
                ProductSlug = l.Product.Slug,
                UnitPrice = l.Product.CurrentPrice,
                OriginalUnitPrice = l.Product.OriginalPrice,
                Quantity = l.Quantity
            });
            var p = await _db.Products.FindAsync(l.Product.Id);
            if (p != null) { p.Stock -= l.Quantity; p.SoldCount += l.Quantity; }
        }

        _db.Orders.Add(order);

        // Save address if requested
        if (input.SaveAddress && !string.IsNullOrWhiteSpace(input.Address))
        {
            var hasAny = await _db.Addresses.AnyAsync(a => a.UserId == user.Id);
            _db.Addresses.Add(new Address
            {
                UserId = user.Id,
                ReceiverName = input.CustomerName.Trim(),
                Phone = input.Phone.Trim(),
                AddressLine = input.Address.Trim(),
                IsDefault = !hasAny
            });
        }

        await _db.SaveChangesAsync();

        // Increment coupon usage
        if (!string.IsNullOrWhiteSpace(cart.CouponCode)) await _coupons.IncrementUsageAsync(cart.CouponCode);

        // Clear cart
        await _cart.ClearAsync();

        // Notify customer
        await _notif.CreateAsync(user.Id, "Đặt hàng thành công", $"Đơn hàng #{code} đã được tạo với tổng {order.Total:N0}đ. Chúng tôi sẽ liên hệ xác nhận.", $"/don-hang/{code}", "✅");

        return (true, code, null);
    }

    public async Task<List<Order>> GetMyOrdersAsync()
    {
        var uid = _auth.GetCurrentId();
        if (uid == null) return new();
        return await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == uid.Value)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<Order?> GetByCodeAsync(string code)
    {
        var user = await _auth.GetCurrentAsync();
        if (user == null) return null;
        return await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.History)
            .FirstOrDefaultAsync(o => o.Code == code && (o.UserId == user.Id || user.Role == "Admin" || user.Role == "Staff"));
    }

    public async Task<Order?> TrackByCodeAndPhoneAsync(string code, string phone)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.History)
            .FirstOrDefaultAsync(o => o.Code.ToUpper() == code.Trim().ToUpper() && o.Phone == phone.Trim());
        return order;
    }

    // ---- Status transitions ----

    private async Task<(bool ok, string msg)> ChangeStatusAsync(int orderId, OrderStatus newStatus, string note, string staffName)
    {
        var o = await _db.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == orderId);
        if (o == null) return (false, "Không tìm thấy đơn");
        var oldStatus = o.Status;
        var allowed = oldStatus switch
        {
            OrderStatus.Pending => new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
            OrderStatus.Confirmed => new[] { OrderStatus.Packing, OrderStatus.Cancelled },
            OrderStatus.Packing => new[] { OrderStatus.Shipping, OrderStatus.Cancelled },
            OrderStatus.Shipping => new[] { OrderStatus.Delivered },
            _ => Array.Empty<OrderStatus>()
        };
        if (!allowed.Contains(newStatus)) return (false, $"Không thể chuyển đơn từ {oldStatus.Label()} sang {newStatus.Label()}");
        if (newStatus == OrderStatus.Delivered && o.PaymentMethod == PaymentMethod.BankTransfer && o.Payment != PaymentStatus.Paid)
            return (false, "Chưa xác minh chuyển khoản, chưa thể hoàn tất giao hàng");

        o.Status = newStatus;
        o.History.Add(new OrderStatusHistory { Status = newStatus, Note = note, ChangedBy = staffName });

        if (newStatus == OrderStatus.Confirmed) o.ConfirmedAt = DateTime.Now;
        if (newStatus == OrderStatus.Shipping) o.ShippedAt = DateTime.Now;
        if (newStatus == OrderStatus.Delivered) { o.DeliveredAt = DateTime.Now; if (o.PaymentMethod == PaymentMethod.COD) o.Payment = PaymentStatus.Paid; }
        if (newStatus == OrderStatus.Cancelled)
        {
            o.CancelledAt = DateTime.Now;
            foreach (var i in o.Items)
            {
                var p = await _db.Products.FindAsync(i.ProductId);
                if (p != null) { p.Stock += i.Quantity; p.SoldCount = Math.Max(0, p.SoldCount - i.Quantity); }
            }
        }
        await _db.SaveChangesAsync();
        try
        {
            var notice = newStatus switch
            {
                OrderStatus.Confirmed => ("Đơn hàng đã được xác nhận", $"Đơn #{o.Code} đã được cửa hàng xác nhận.", "✅"),
                OrderStatus.Packing => ("Đơn hàng đang được đóng gói", $"Đơn #{o.Code} đang được chuẩn bị để giao.", "📦"),
                OrderStatus.Shipping => ("Đơn hàng đang giao", $"Đơn #{o.Code} đã được bàn giao vận chuyển.", "🚚"),
                OrderStatus.Delivered => ("Đơn hàng đã giao thành công", $"Đơn #{o.Code} đã được đánh dấu giao thành công.", "🎉"),
                OrderStatus.Cancelled => ("Đơn hàng đã bị hủy", $"Đơn #{o.Code} đã bị hủy. Vui lòng liên hệ cửa hàng nếu cần hỗ trợ.", "⚠️"),
                _ => ("Cập nhật đơn hàng", $"Đơn #{o.Code} đã cập nhật trạng thái.", "🔔")
            };
            await _notif.CreateAsync(o.UserId, notice.Item1, notice.Item2, $"/don-hang/{o.Code}", notice.Item3);
        }
        catch
        {
            // A notification failure should never undo a saved order transition.
        }
        return (true, $"Đã chuyển sang {newStatus.Label()}");
    }

    public Task<(bool ok, string msg)> ConfirmAsync(int id, string staff) => ChangeStatusAsync(id, OrderStatus.Confirmed, "Đã xác nhận đơn hàng", staff);
    public Task<(bool ok, string msg)> PackAsync(int id, string staff) => ChangeStatusAsync(id, OrderStatus.Packing, "Đang đóng gói", staff);
    public Task<(bool ok, string msg)> ShipAsync(int id, string staff) => ChangeStatusAsync(id, OrderStatus.Shipping, "Đang giao hàng", staff);
    public Task<(bool ok, string msg)> DeliverAsync(int id, string staff) => ChangeStatusAsync(id, OrderStatus.Delivered, "Đã giao hàng thành công", staff);
    public Task<(bool ok, string msg)> CancelAsync(int id, string staff) => ChangeStatusAsync(id, OrderStatus.Cancelled, "Đơn hàng bị hủy", staff);

    public async Task<(bool ok, string msg)> SetPaymentAsync(int id, PaymentStatus status)
    {
        var o = await _db.Orders.FindAsync(id);
        if (o == null) return (false, "Không tìm thấy");
        if (status == PaymentStatus.Refunded && o.Payment != PaymentStatus.Paid) return (false, "Chỉ hoàn tiền cho đơn đã thanh toán");
        o.Payment = status;
        await _db.SaveChangesAsync();
        return (true, "Đã cập nhật thanh toán");
    }

    public async Task<(bool ok, string msg)> SetTrackingAsync(int id, string trackingNumber, string changedBy = "Staff")
    {
        var o = await _db.Orders.Include(x => x.History).FirstOrDefaultAsync(x => x.Id == id);
        if (o == null) return (false, "Không tìm thấy");
        var normalized = (trackingNumber ?? "").Trim();
        if (normalized.Length == 0) return (false, "Vui lòng nhập mã vận đơn");
        if (normalized.Length > 120) return (false, "Mã vận đơn tối đa 120 ký tự");
        if (string.Equals(o.TrackingNumber, normalized, StringComparison.Ordinal))
            return (true, "Mã vận đơn không thay đổi");
        o.TrackingNumber = normalized;
        o.History.Add(new OrderStatusHistory
        {
            Status = o.Status,
            Note = $"Cập nhật mã vận đơn: {normalized}",
            ChangedBy = string.IsNullOrWhiteSpace(changedBy) ? "Staff" : changedBy,
            CreatedAt = DateTime.Now
        });
        await _db.SaveChangesAsync();
        return (true, "Đã cập nhật mã vận đơn và ghi lịch sử xử lý");
    }
}


