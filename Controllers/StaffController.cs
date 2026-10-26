using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovaShop.Data;
using NovaShop.Models;
using NovaShop.Services;

namespace NovaShop.Controllers;

[Authorize(Roles = "Staff,Admin")]
public class StaffController : Controller
{
    private readonly AppDbContext _db;
    private readonly ReportService _reports;
    private readonly OrderService _orders;
    private readonly AuthService _auth;
    private readonly AuditService _audit;
    private readonly NotificationService _notifications;

    public StaffController(AppDbContext db, ReportService reports, OrderService orders, AuthService auth, AuditService audit, NotificationService notifications)
    { _db = db; _reports = reports; _orders = orders; _auth = auth; _audit = audit; _notifications = notifications; }

    [Route("staff")]
    public async Task<IActionResult> Index() => View(await _reports.GetDashboardAsync());

    [Route("staff/kanban")]
    public async Task<IActionResult> Kanban()
    {
        var orders = await _db.Orders.Include(o => o.Items).OrderBy(o => o.CreatedAt).ToListAsync();
        ViewBag.Pending = orders.Where(o => o.Status == OrderStatus.Pending).ToList();
        ViewBag.Confirmed = orders.Where(o => o.Status == OrderStatus.Confirmed).ToList();
        ViewBag.Packing = orders.Where(o => o.Status == OrderStatus.Packing).ToList();
        ViewBag.Shipping = orders.Where(o => o.Status == OrderStatus.Shipping).ToList();
        ViewBag.Delivered = orders.Where(o => o.Status == OrderStatus.Delivered).OrderByDescending(o => o.DeliveredAt).Take(10).ToList();
        return View();
    }

    [Route("staff/don-hang")]
    public async Task<IActionResult> Orders(OrderStatus? status, string? q)
    {
        var query = _db.Orders.AsNoTracking().Include(o => o.Items).AsQueryable();
        if (status.HasValue) query = query.Where(o => o.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(o => o.Code.Contains(q) || o.CustomerName.Contains(q) || o.Phone.Contains(q));
        ViewBag.Status = status; ViewBag.Search = q;
        return View(await query.OrderByDescending(o => o.CreatedAt).Take(300).ToListAsync());
    }

    [HttpGet, Route("staff/don-hang/xuat-csv")]
    public async Task<IActionResult> ExportOrdersCsv(OrderStatus? status, string? q)
    {
        var query = _db.Orders.AsNoTracking().AsQueryable();
        if (status.HasValue) query = query.Where(o => o.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(o => o.Code.Contains(q) || o.CustomerName.Contains(q) || o.Phone.Contains(q));
        var rows = await query.OrderByDescending(o => o.CreatedAt).Take(5000).ToListAsync();
        var csv = new StringBuilder("Ma don,Ngay tao,Khach hang,Dien thoai,Dia chi,Trang thai,Thanh toan,Tong tien,Ma van don\r\n");
        foreach (var o in rows)
        {
            string Cell(string? value) => "\"" + (value ?? "").Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";
            csv.AppendLine(string.Join(",", new[] { o.Code, o.CreatedAt.ToString("yyyy-MM-dd HH:mm"), o.CustomerName, o.Phone,
                o.Address, o.Status.Label(), o.Payment.Label(), o.Total.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                o.TrackingNumber ?? "" }.Select(Cell)));
        }
        await _audit.LogAsync(CurrentEmail(), "Xuất CSV đơn hàng", "Order", "*", $"{rows.Count} đơn");
        return File(new UTF8Encoding(true).GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"btvh-don-hang-{DateTime.Now:yyyyMMdd-HHmm}.csv");
    }

    [HttpGet, Route("staff/don-hang/{id:int}")]
    public async Task<IActionResult> OrderDetail(int id)
    {
        var order = await _db.Orders.AsNoTracking().Include(o => o.Items).Include(o => o.History).FirstOrDefaultAsync(o => o.Id == id);
        return order == null ? NotFound() : View(order);
    }

    [Route("staff/thanh-toan")]
    public async Task<IActionResult> Payments(string? q)
    {
        if (!await MayVerifyPaymentsAsync()) return Forbid();
        var query = _db.Orders.AsNoTracking().Include(o => o.Items)
            .Where(o => o.PaymentMethod == PaymentMethod.BankTransfer && o.Payment == PaymentStatus.Unpaid && o.Status != OrderStatus.Cancelled).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(o => o.Code.Contains(q) || o.CustomerName.Contains(q) || o.Phone.Contains(q));
        ViewBag.Search = q;
        return View(await query.OrderBy(o => o.CreatedAt).Take(250).ToListAsync());
    }

    [HttpPost, Route("staff/thanh-toan/{id:int}/xac-minh")]
    public async Task<IActionResult> VerifyPayment(int id, bool approved, string? note)
    {
        if (!await MayVerifyPaymentsAsync()) return Forbid();
        var order = await _db.Orders.FindAsync(id);
        if (order == null) { TempData["Error"] = "Không tìm thấy đơn hàng"; return RedirectToAction(nameof(Payments)); }
        if (order.PaymentMethod != PaymentMethod.BankTransfer || order.Status == OrderStatus.Cancelled || order.Payment != PaymentStatus.Unpaid)
        { TempData["Error"] = "Đơn này không còn thuộc hàng chờ xác minh chuyển khoản"; return RedirectToAction(nameof(Payments)); }
        if (approved) order.Payment = PaymentStatus.Paid;
        order.PaymentNote = note?.Trim();
        await _db.SaveChangesAsync();
        if (approved)
        {
            try { await _notifications.CreateAsync(order.UserId, "Đã xác minh thanh toán", $"Khoản chuyển khoản cho đơn #{order.Code} đã được xác nhận.", $"/don-hang/{order.Code}", "💳"); }
            catch { /* Notification failure does not cancel payment verification. */ }
        }
        await _audit.LogAsync(CurrentEmail(), approved ? "Xác minh chuyển khoản" : "Từ chối xác minh chuyển khoản", "Order", id.ToString(), $"{order.Code}: {note}");
        TempData[approved ? "Success" : "Error"] = approved ? $"Đã xác minh đơn #{order.Code}" : $"Đã ghi chú đơn #{order.Code}; trạng thái vẫn chờ thanh toán";
        return RedirectToAction(nameof(Payments));
    }

    [HttpPost, Route("staff/don-hang/{id:int}/van-don")]
    public async Task<IActionResult> UpdateTracking(int id, string trackingNumber)
    {
        var result = await _orders.SetTrackingAsync(id, trackingNumber ?? "", CurrentEmail());
        TempData[result.ok ? "Success" : "Error"] = result.msg;
        await _audit.LogAsync(CurrentEmail(), "Cập nhật mã vận đơn", "Order", id.ToString(), trackingNumber ?? "");
        return RedirectToAction(nameof(OrderDetail), new { id });
    }

    [Route("staff/ton-kho")]
    public async Task<IActionResult> Inventory(string? q, bool lowOnly = true)
    {
        var query = _db.Products.AsNoTracking().Include(p => p.Category).Include(p => p.Brand).Include(p => p.Images).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(p => p.Name.Contains(q));
        if (lowOnly) query = query.Where(p => p.Stock <= 5);
        ViewBag.Search = q; ViewBag.LowOnly = lowOnly;
        return View(await query.OrderBy(p => p.Stock).ThenBy(p => p.Name).Take(300).ToListAsync());
    }

    [HttpPost, Route("staff/don/{id:int}/xac-nhan")]
    public async Task<IActionResult> Confirm(int id) => await DoStatusAsync(() => _orders.ConfirmAsync(id, CurrentEmail()), id);
    [HttpPost, Route("staff/don/{id:int}/dong-goi")]
    public async Task<IActionResult> Pack(int id) => await DoStatusAsync(() => _orders.PackAsync(id, CurrentEmail()), id);
    [HttpPost, Route("staff/don/{id:int}/giao")]
    public async Task<IActionResult> Ship(int id) => await DoStatusAsync(() => _orders.ShipAsync(id, CurrentEmail()), id);
    [HttpPost, Route("staff/don/{id:int}/giao-xong")]
    public async Task<IActionResult> Deliver(int id) => await DoStatusAsync(() => _orders.DeliverAsync(id, CurrentEmail()), id);
    [HttpPost, Route("staff/don/{id:int}/huy")]
    public async Task<IActionResult> Cancel(int id) => await DoStatusAsync(() => _orders.CancelAsync(id, CurrentEmail()), id);

    private async Task<IActionResult> DoStatusAsync(Func<Task<(bool ok, string msg)>> update, int id)
    {
        var result = await update(); TempData[result.ok ? "Success" : "Error"] = result.msg;
        if (result.ok) await _audit.LogAsync(CurrentEmail(), "Cập nhật trạng thái đơn", "Order", id.ToString(), result.msg);
        return RedirectToAction(nameof(Kanban));
    }

    private async Task<bool> MayVerifyPaymentsAsync()
    {
        if (User.IsInRole("Admin")) return true;
        var user = await _auth.GetCurrentAsync();
        return user?.Role == "Staff" && user.CanVerifyPayments && user.IsActive;
    }

    private string CurrentEmail() => User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name ?? "system";
}
