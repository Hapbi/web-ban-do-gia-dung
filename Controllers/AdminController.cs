using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using NovaShop.Data;
using NovaShop.Models;
using NovaShop.Services;

namespace NovaShop.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    private readonly ReportService _reports;
    private readonly MarketplaceService _marketplace;
    private readonly SettingsService _settings;
    private readonly AuditService _audit;
    private readonly OrderService _orders;
    private readonly NotificationService _notifications;
    public AdminController(AppDbContext db, ReportService reports, MarketplaceService marketplace, SettingsService settings, AuditService audit, OrderService orders, NotificationService notifications)
    { _db = db; _reports = reports; _marketplace = marketplace; _settings = settings; _audit = audit; _orders = orders; _notifications = notifications; }

    [Route("admin")]
    public async Task<IActionResult> Index() => View(await _reports.GetDashboardAsync());

    // ---- PRODUCTS ----
    [Route("admin/san-pham")]
    public async Task<IActionResult> Products(string? q, int? catId, bool? active, bool lowStockOnly = false, int page = 1)
    {
        var query = _db.Products.Include(p => p.Category).Include(p => p.Brand).Include(p => p.Images).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(p => p.Name.Contains(q));
        if (catId.HasValue) query = query.Where(p => p.CategoryId == catId.Value);
        if (active.HasValue) query = query.Where(p => p.IsActive == active.Value);
        if (lowStockOnly) query = query.Where(p => p.Stock <= 5);

        var total = await query.CountAsync();
        var products = await query.OrderByDescending(p => p.Id).Skip((page - 1) * 20).Take(20).ToListAsync();
        return View(new AdminProductListVM
        {
            Products = products, Search = q, CategoryId = catId, IsActive = active, LowStockOnly = lowStockOnly,
            Categories = await _db.Categories.ToListAsync(), Page = page, Total = total
        });
    }

    [HttpGet, Route("admin/san-pham/them")]
    [HttpGet, Route("admin/san-pham/{id}/sua")]
    public async Task<IActionResult> ProductEdit(int? id)
    {
        ProductEditVM vm;
        if (id == null)
        {
            vm = new ProductEditVM { IsActive = true };
        }
        else
        {
            var p = await _db.Products.Include(x => x.Images).Include(x => x.Specs).FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return NotFound();
            vm = new ProductEditVM
            {
                Id = p.Id, Name = p.Name, Slug = p.Slug, ShortDescription = p.ShortDescription,
                Description = p.Description, CategoryId = p.CategoryId, BrandId = p.BrandId,
                OriginalPrice = p.OriginalPrice, SalePrice = p.SalePrice, Stock = p.Stock,
                IsActive = p.IsActive, IsFeatured = p.IsFeatured, Tags = p.Tags,
                ImageUrls = p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).ToList(),
                Specs = p.Specs.OrderBy(s => s.SortOrder).Select(s => (s.Name, s.Value)).ToList()
            };
        }
        vm.Categories = await _db.Categories.ToListAsync();
        vm.Brands = await _db.Brands.ToListAsync();
        return View(vm);
    }

    [HttpPost, Route("admin/san-pham/luu")]
    public async Task<IActionResult> ProductSave(ProductEditVM vm, string? imageUrls, string? specsText)
    {
        var categoryExists = vm.CategoryId > 0 && await _db.Categories.AnyAsync(c => c.Id == vm.CategoryId);
        var brandExists = vm.BrandId > 0 && await _db.Brands.AnyAsync(b => b.Id == vm.BrandId);
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(vm.Name) || vm.OriginalPrice <= 0 || vm.Stock < 0
            || !categoryExists || !brandExists || (vm.SalePrice.HasValue && (vm.SalePrice.Value <= 0 || vm.SalePrice.Value >= vm.OriginalPrice)))
        {
            TempData["Error"] = "Thông tin sản phẩm chưa hợp lệ. Hãy kiểm tra danh mục, thương hiệu, giá và tồn kho.";
            vm.Categories = await _db.Categories.ToListAsync();
            vm.Brands = await _db.Brands.ToListAsync();
            return View("ProductEdit", vm);
        }

        Product p;
        if (vm.Id == 0)
        {
            p = new Product();
            p.Slug = Slugify(vm.Name);
            _db.Products.Add(p);
        }
        else
        {
            p = (await _db.Products.Include(x => x.Images).Include(x => x.Specs).FirstOrDefaultAsync(x => x.Id == vm.Id))!;
            if (p == null) return NotFound();
            _db.ProductImages.RemoveRange(p.Images);
            _db.ProductSpecs.RemoveRange(p.Specs);
            p.Images.Clear();
            p.Specs.Clear();
        }

        p.Name = vm.Name.Trim();
        p.ShortDescription = vm.ShortDescription?.Trim() ?? "";
        p.Description = vm.Description?.Trim() ?? "";
        p.CategoryId = vm.CategoryId;
        p.BrandId = vm.BrandId;
        p.OriginalPrice = vm.OriginalPrice;
        p.SalePrice = vm.SalePrice;
        p.Stock = vm.Stock;
        p.IsActive = p.IsMarketplaceListing && p.ListingStatus != ListingStatus.Approved ? false : vm.IsActive;
        p.IsFeatured = vm.IsFeatured;
        p.Tags = vm.Tags ?? "";
        p.UpdatedAt = DateTime.Now;

        // Images
        if (!string.IsNullOrWhiteSpace(imageUrls))
        {
            var urls = imageUrls.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < urls.Length; i++)
                p.Images.Add(new ProductImage { Url = urls[i].Trim(), IsPrimary = i == 0, SortOrder = i });
        }
        // Specs
        if (!string.IsNullOrWhiteSpace(specsText))
        {
            var lines = specsText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                var idx = lines[i].IndexOf(':');
                if (idx > 0)
                    p.Specs.Add(new ProductSpec { Name = lines[i][..idx].Trim(), Value = lines[i][(idx + 1)..].Trim(), SortOrder = i });
            }
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentEmail(), vm.Id == 0 ? "Tạo sản phẩm" : "Cập nhật sản phẩm", "Product", p.Id.ToString(), p.Name);
        TempData["Success"] = "Đã lưu sản phẩm";
        return RedirectToAction(nameof(Products));
    }

    [HttpPost, Route("admin/san-pham/{id}/xoa")]
    public async Task<IActionResult> ProductDelete(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p != null) { p.IsActive = false; p.UpdatedAt = DateTime.Now; await _db.SaveChangesAsync(); await _audit.LogAsync(CurrentEmail(), "Ẩn sản phẩm", "Product", id.ToString(), p.Name); TempData["Success"] = "Đã ẩn sản phẩm; đơn hàng cũ vẫn được giữ nguyên"; }
        return RedirectToAction(nameof(Products));
    }

    [HttpPost, Route("admin/san-pham/{id}/an")]
    public async Task<IActionResult> ProductToggle(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p != null)
        {
            if (p.IsMarketplaceListing && p.ListingStatus != ListingStatus.Approved && !p.IsActive)
            {
                TempData["Error"] = "Listing Marketplace phải được duyệt trước khi hiển thị công khai.";
                return RedirectToAction(nameof(Products));
            }
            p.IsActive = !p.IsActive; p.UpdatedAt = DateTime.Now; await _db.SaveChangesAsync(); await _audit.LogAsync(CurrentEmail(), p.IsActive ? "Hiện sản phẩm" : "Ẩn sản phẩm", "Product", id.ToString(), p.Name);
        }
        return RedirectToAction(nameof(Products));
    }

    // ---- ORDERS ----
    [Route("admin/don-hang")]
    public async Task<IActionResult> Orders(OrderStatus? status, string? q, int page = 1)
    {
        var query = _db.Orders.Include(o => o.Items).AsQueryable();
        if (status.HasValue) query = query.Where(o => o.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(o => o.Code.Contains(q) || o.CustomerName.Contains(q) || o.Phone.Contains(q));

        var total = await query.CountAsync();
        var orders = await query.OrderByDescending(o => o.CreatedAt).Skip((page - 1) * 20).Take(20).ToListAsync();

        return View(new AdminOrderListVM
        {
            Orders = orders, Status = status, Search = q, Page = page, Total = total,
            StatusCounts = await _db.Orders.GroupBy(o => o.Status).ToDictionaryAsync(g => g.Key, g => g.Count())
        });
    }

    [HttpGet, Route("admin/don-hang/xuat-csv")]
    public async Task<IActionResult> ExportOrdersCsv(OrderStatus? status, string? q)
    {
        var query = _db.Orders.AsNoTracking().AsQueryable();
        if (status.HasValue) query = query.Where(o => o.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(o => o.Code.Contains(q) || o.CustomerName.Contains(q) || o.Phone.Contains(q));
        var rows = await query.OrderByDescending(o => o.CreatedAt).Take(10000).ToListAsync();
        var csv = new StringBuilder();
        csv.AppendLine("Ma don,Ngay tao,Khach hang,Email,Dien thoai,Dia chi,Trang thai,Thanh toan,Phuong thuc,Tam tinh,Giam gia,Phi ship,Tong tien,Ma van don");
        foreach (var o in rows)
            csv.AppendLine(string.Join(",", new[] { o.Code, o.CreatedAt.ToString("yyyy-MM-dd HH:mm"), o.CustomerName, o.Email,
                o.Phone, o.Address, o.Status.Label(), o.Payment.Label(), o.PaymentMethod.Label(),
                o.Subtotal.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                o.Discount.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                o.Shipping.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                o.Total.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture), o.TrackingNumber ?? "" }.Select(CsvCell)));
        var bytes = new UTF8Encoding(true).GetBytes(csv.ToString());
        await _audit.LogAsync(CurrentEmail(), "Xuất CSV đơn hàng", "Order", "*", $"{rows.Count} đơn");
        return File(bytes, "text/csv; charset=utf-8", $"btvh-don-hang-{DateTime.Now:yyyyMMdd-HHmm}.csv");
    }

    [HttpGet, Route("admin/don-hang/{id:int}")]
    public async Task<IActionResult> OrderDetail(int id)
    {
        var o = await _db.Orders.Include(x => x.Items).Include(x => x.History).Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);
        if (o == null) return NotFound();
        return View(o);
    }

    // ---- USERS ----
    [Route("admin/nguoi-dung")]
    public async Task<IActionResult> Users(string? role, string? q)
    {
        var query = _db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(role)) query = query.Where(u => u.Role == role);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(u => u.Email.Contains(q) || u.FullName.Contains(q));
        ViewBag.RoleFilter = role;
        ViewBag.Search = q;
        return View(await query.OrderByDescending(u => u.CreatedAt).ToListAsync());
    }

    [HttpPost, Route("admin/nguoi-dung/{id}/role")]
    public async Task<IActionResult> SetRole(int id, string role)
    {
        var u = await _db.Users.FindAsync(id);
        if (u != null && new[] { "Admin", "Staff", "Customer" }.Contains(role))
        {
            if (string.Equals(u.Email, CurrentEmail(), StringComparison.OrdinalIgnoreCase) && role != "Admin")
            { TempData["Error"] = "Không thể tự hạ quyền tài khoản quản trị đang đăng nhập"; return RedirectToAction(nameof(Users)); }
            if (u.Role == "Admin" && role != "Admin" && u.IsActive && await _db.Users.CountAsync(x => x.Role == "Admin" && x.IsActive) <= 1)
            { TempData["Error"] = "Không thể hạ quyền quản trị viên đang hoạt động cuối cùng"; return RedirectToAction(nameof(Users)); }
            u.Role = role;
            if (role != "Staff") u.CanVerifyPayments = false;
            await _db.SaveChangesAsync();
            await _audit.LogAsync(CurrentEmail(), "Đổi vai trò người dùng", "User", id.ToString(), $"{u.Email}: {role}");
            TempData["Success"] = "Đã cập nhật vai trò";
        }
        return RedirectToAction(nameof(Users));
    }

    [HttpPost, Route("admin/nguoi-dung/{id}/khoa")]
    public async Task<IActionResult> ToggleLock(int id)
    {
        var u = await _db.Users.FindAsync(id);
        if (u != null)
        {
            var currentEmail = CurrentEmail();
            if (string.Equals(u.Email, currentEmail, StringComparison.OrdinalIgnoreCase)) { TempData["Error"] = "Anh/chị không thể tự khóa tài khoản đang đăng nhập"; return RedirectToAction(nameof(Users)); }
            if (u.Role == "Admin" && u.IsActive && await _db.Users.CountAsync(x => x.Role == "Admin" && x.IsActive) <= 1) { TempData["Error"] = "Không thể khóa quản trị viên đang hoạt động cuối cùng"; return RedirectToAction(nameof(Users)); }
            u.IsActive = !u.IsActive; await _db.SaveChangesAsync();
            await _audit.LogAsync(CurrentEmail(), u.IsActive ? "Mở khóa người dùng" : "Khóa người dùng", "User", id.ToString(), u.Email);
            TempData["Success"] = "Đã cập nhật trạng thái tài khoản";
        }
        return RedirectToAction(nameof(Users));
    }

    // ---- BLOG ----
    [Route("admin/blog")]
    public async Task<IActionResult> Blog() => View(await _db.BlogPosts.OrderByDescending(b => b.CreatedAt).ToListAsync());

    [HttpGet, Route("admin/blog/them")]
    [HttpGet, Route("admin/blog/{id}/sua")]
    public async Task<IActionResult> BlogEdit(int? id)
    {
        if (id == null) return View(new BlogPost());
        var p = await _db.BlogPosts.FindAsync(id);
        return p == null ? NotFound() : View(p);
    }

    [HttpPost, Route("admin/blog/luu")]
    public async Task<IActionResult> BlogSave(BlogPost input)
    {
        if (string.IsNullOrWhiteSpace(input.Title)) { TempData["Error"] = "Vui lòng nhập tiêu đề"; return RedirectToAction(nameof(Blog)); }
        BlogPost p;
        if (input.Id == 0) { p = new BlogPost { Slug = Slugify(input.Title) }; _db.BlogPosts.Add(p); }
        else { p = (await _db.BlogPosts.FindAsync(input.Id))!; if (p == null) return NotFound(); }

        p.Title = input.Title.Trim();
        p.Summary = input.Summary?.Trim() ?? "";
        p.Content = input.Content ?? "";
        p.CoverImage = input.CoverImage ?? "";
        p.AuthorName = input.AuthorName ?? "Admin";
        p.Category = input.Category ?? "";
        p.Tags = input.Tags ?? "";
        p.IsPublished = input.IsPublished;
        if (input.IsPublished && p.PublishedAt == null) p.PublishedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentEmail(), "Lưu bài viết", "BlogPost", p.Id.ToString(), p.Title);
        TempData["Success"] = "Đã lưu bài viết";
        return RedirectToAction(nameof(Blog));
    }

    [HttpPost, Route("admin/blog/{id}/xoa")]
    public async Task<IActionResult> BlogDelete(int id)
    {
        var p = await _db.BlogPosts.FindAsync(id);
        if (p != null) { var title = p.Title; _db.BlogPosts.Remove(p); await _db.SaveChangesAsync(); await _audit.LogAsync(CurrentEmail(), "Xóa bài viết", "BlogPost", id.ToString(), title); TempData["Success"] = "Đã xóa"; }
        return RedirectToAction(nameof(Blog));
    }

    // ---- COUPONS ----
    [Route("admin/ma-giam-gia")]
    public async Task<IActionResult> Coupons() => View(await _db.Coupons.OrderByDescending(c => c.Id).ToListAsync());

    [HttpPost, Route("admin/ma-giam-gia/them")]
    public async Task<IActionResult> AddCoupon(Coupon c)
    {
        if (!string.IsNullOrWhiteSpace(c.Code))
        {
            c.Code = c.Code.Trim().ToUpper();
            if (!await _db.Coupons.AnyAsync(x => x.Code == c.Code))
            {
                _db.Coupons.Add(c);
                await _db.SaveChangesAsync();
                await _audit.LogAsync(CurrentEmail(), "Tạo mã giảm giá", "Coupon", c.Id.ToString(), c.Code);
                TempData["Success"] = "Đã thêm mã giảm giá";
            }
            else TempData["Error"] = "Mã đã tồn tại";
        }
        return RedirectToAction(nameof(Coupons));
    }

    [HttpPost, Route("admin/ma-giam-gia/{id}/xoa")]
    public async Task<IActionResult> DeleteCoupon(int id)
    {
        var c = await _db.Coupons.FindAsync(id);
        if (c != null) { var code = c.Code; _db.Coupons.Remove(c); await _db.SaveChangesAsync(); await _audit.LogAsync(CurrentEmail(), "Xóa mã giảm giá", "Coupon", id.ToString(), code); TempData["Success"] = "Đã xóa"; }
        return RedirectToAction(nameof(Coupons));
    }

    // ---- MESSAGES ----
    [Route("admin/tin-nhan")]
    public async Task<IActionResult> Messages() =>
        View(await _db.ContactMessages.OrderByDescending(m => m.CreatedAt).ToListAsync());

    [HttpPost, Route("admin/tin-nhan/{id}/doc")]
    public async Task<IActionResult> MarkMessageRead(int id)
    {
        var m = await _db.ContactMessages.FindAsync(id);
        if (m != null) { m.IsRead = true; await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Messages));
    }

    // ---- REVIEWS ----
    [Route("admin/danh-gia")]
    public async Task<IActionResult> Reviews() =>
        View(await _db.Reviews.Include(r => r.User).Include(r => r.Product).OrderByDescending(r => r.CreatedAt).ToListAsync());

    // ---- PAYMENT VERIFICATION ----
    [Route("admin/thanh-toan")]
    public async Task<IActionResult> Payments(string? q, bool pendingOnly = true)
    {
        var query = _db.Orders.AsNoTracking().Where(o => o.PaymentMethod == PaymentMethod.BankTransfer)
            .Include(o => o.Items).AsQueryable();
        if (pendingOnly) query = query.Where(o => o.Payment == PaymentStatus.Unpaid && o.Status != OrderStatus.Cancelled);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(o => o.Code.Contains(q) || o.CustomerName.Contains(q) || o.Phone.Contains(q));
        ViewBag.Search = q; ViewBag.PendingOnly = pendingOnly;
        ViewBag.PendingCount = await _db.Orders.CountAsync(o => o.PaymentMethod == PaymentMethod.BankTransfer && o.Payment == PaymentStatus.Unpaid && o.Status != OrderStatus.Cancelled);
        return View(await query.OrderByDescending(o => o.CreatedAt).Take(250).ToListAsync());
    }

    [HttpPost, Route("admin/thanh-toan/{id}/cap-nhat")]
    public async Task<IActionResult> UpdatePayment(int id, PaymentStatus status, string? note)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) { TempData["Error"] = "Không tìm thấy đơn hàng"; return RedirectToAction(nameof(Payments)); }
        if (!Enum.IsDefined(status) || status == PaymentStatus.Unpaid)
        { TempData["Error"] = "Trạng thái thanh toán không hợp lệ"; return RedirectToAction(nameof(Payments)); }
        if (order.PaymentMethod != PaymentMethod.BankTransfer)
        { TempData["Error"] = "Trang này chỉ xử lý đơn chuyển khoản"; return RedirectToAction(nameof(Payments)); }
        if (status == PaymentStatus.Paid && order.Status == OrderStatus.Cancelled)
        { TempData["Error"] = "Không thể xác nhận thanh toán cho đơn đã hủy"; return RedirectToAction(nameof(Payments)); }
        if (status == PaymentStatus.Refunded && order.Payment != PaymentStatus.Paid)
        { TempData["Error"] = "Chỉ có thể đánh dấu hoàn tiền cho đơn đã thanh toán"; return RedirectToAction(nameof(Payments)); }
        var oldPayment = order.Payment;
        order.Payment = status; order.PaymentNote = note?.Trim();
        await _db.SaveChangesAsync();
        if (oldPayment != status && status is PaymentStatus.Paid or PaymentStatus.Refunded)
        {
            try
            {
                var paid = status == PaymentStatus.Paid;
                await _notifications.CreateAsync(order.UserId, paid ? "Đã xác minh thanh toán" : "Đã cập nhật hoàn tiền",
                    paid ? $"Khoản thanh toán cho đơn #{order.Code} đã được xác nhận." : $"Trạng thái hoàn tiền của đơn #{order.Code} đã được cập nhật. Vui lòng liên hệ cửa hàng để biết thêm chi tiết.",
                    $"/don-hang/{order.Code}", paid ? "💳" : "↩️");
            }
            catch { /* Notification failure does not cancel the payment update. */ }
        }
        await _audit.LogAsync(CurrentEmail(), "Cập nhật thanh toán", "Order", order.Id.ToString(), $"{order.Code}: {status}; {note}");
        TempData["Success"] = $"Đã cập nhật thanh toán đơn #{order.Code}";
        return RedirectToAction(nameof(Payments));
    }

    // ---- MARKETPLACE MODERATION ----
    [Route("admin/marketplace")]
    public async Task<IActionResult> Marketplace(ListingStatus? status)
    {
        var listings = await _marketplace.GetAllListingsAsync();
        ViewBag.Status = status;
        if (status.HasValue) listings = listings.Where(p => p.ListingStatus == status.Value).ToList();
        return View(listings);
    }

    [HttpPost, Route("admin/marketplace/{id}/duyet")]
    public async Task<IActionResult> ApproveListing(int id)
    {
        var result = await _marketplace.ApproveAsync(id);
        TempData[result.ok ? "Success" : "Error"] = result.msg;
        if (result.ok) await _audit.LogAsync(CurrentEmail(), "Duyệt listing", "Product", id.ToString(), "Marketplace listing approved");
        return RedirectToAction(nameof(Marketplace));
    }

    [HttpPost, Route("admin/marketplace/{id}/tu-choi")]
    public async Task<IActionResult> RejectListing(int id, string reason)
    {
        var result = await _marketplace.RejectAsync(id, reason);
        TempData[result.ok ? "Success" : "Error"] = result.msg;
        if (result.ok) await _audit.LogAsync(CurrentEmail(), "Từ chối listing", "Product", id.ToString(), reason);
        return RedirectToAction(nameof(Marketplace));
    }

    // ---- OPERATIONS & REVENUE REPORTS ----
    [Route("admin/bao-cao/van-hanh")]
    public async Task<IActionResult> OperationsReport() => View(await _reports.GetOperationsAsync());

    [Route("admin/bao-cao/doanh-thu")]
    public async Task<IActionResult> RevenueReport(DateTime? from, DateTime? to)
    {
        var today = DateTime.Today;
        var start = (from ?? new DateTime(today.Year, today.Month, 1)).Date;
        var end = (to ?? today).Date;
        if (start > end) (start, end) = (end, start);
        if ((end - start).TotalDays > 366) start = end.AddDays(-366);
        return View(await _reports.GetRevenueAsync(start, end));
    }

    // ---- SHOP / BANK SETTINGS ----
    [HttpGet, Route("admin/bao-cao/doanh-thu/xuat-csv")]
    public async Task<IActionResult> ExportRevenueCsv(DateTime? from, DateTime? to)
    {
        var today = DateTime.Today;
        var start = (from ?? new DateTime(today.Year, today.Month, 1)).Date;
        var end = (to ?? today).Date;
        if (start > end) (start, end) = (end, start);
        if ((end - start).TotalDays > 366) start = end.AddDays(-366);
        var report = await _reports.GetRevenueAsync(start, end);
        var csv = new StringBuilder("Nhom,Ten,So luong,So don,Doanh thu (VND)\r\n");
        foreach (var d in report.Daily)
            csv.AppendLine(string.Join(",", new[] { CsvCell("Ngay"), CsvCell(d.Date.ToString("yyyy-MM-dd")),
                CsvCell(""), CsvCell(d.Orders.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                CsvCell(d.Revenue.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)) }));
        foreach (var p in report.TopProducts)
            csv.AppendLine(string.Join(",", new[] { CsvCell("San pham"), CsvCell(p.ProductName),
                CsvCell(p.Qty.ToString(System.Globalization.CultureInfo.InvariantCulture)), CsvCell(""),
                CsvCell(p.Revenue.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)) }));
        foreach (var c in report.ByCategory)
            csv.AppendLine(string.Join(",", new[] { CsvCell("Danh muc"), CsvCell(c.CategoryName), CsvCell(""), CsvCell(""),
                CsvCell(c.Revenue.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)) }));
        csv.AppendLine();
        csv.AppendLine($"Tong doanh thu da thu,,,,{CsvCell(report.TotalRevenue.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture))}");
        csv.AppendLine($"Phi Marketplace uoc tinh,,,,{CsvCell(report.PlatformFees.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture))}");
        await _audit.LogAsync(CurrentEmail(), "Xuất CSV báo cáo doanh thu", "RevenueReport", "*", $"{start:yyyy-MM-dd} đến {end:yyyy-MM-dd}");
        return File(new UTF8Encoding(true).GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"btvh-doanh-thu-{start:yyyyMMdd}-{end:yyyyMMdd}.csv");
    }

    [Route("admin/cau-hinh")]
    public async Task<IActionResult> Settings() => View(new AdminSettingsVM { Site = await _settings.GetSiteAsync(), Bank = await _settings.GetBankAsync() });

    [HttpPost, Route("admin/cau-hinh/cua-hang")]
    public async Task<IActionResult> SaveSiteSettings(SiteSettings input)
    {
        if (string.IsNullOrWhiteSpace(input.SiteName) || string.IsNullOrWhiteSpace(input.Email))
        { TempData["Error"] = "Tên cửa hàng và email hỗ trợ là bắt buộc"; return RedirectToAction(nameof(Settings)); }
        await _settings.SaveSiteAsync(input);
        await _audit.LogAsync(CurrentEmail(), "Cập nhật cấu hình", "SiteSettings", "1", input.SiteName);
        TempData["Success"] = "Đã lưu thông tin cửa hàng";
        return RedirectToAction(nameof(Settings));
    }

    [HttpPost, Route("admin/cau-hinh/ngan-hang")]
    public async Task<IActionResult> SaveBankSettings(BankSettings input)
    {
        if (string.IsNullOrWhiteSpace(input.BankName)) { TempData["Error"] = "Vui lòng nhập tên ngân hàng"; return RedirectToAction(nameof(Settings)); }
        await _settings.SaveBankAsync(input);
        await _audit.LogAsync(CurrentEmail(), "Cập nhật cấu hình ngân hàng", "BankSettings", "1", input.BankName);
        TempData["Success"] = "Đã lưu cấu hình ngân hàng";
        return RedirectToAction(nameof(Settings));
    }

    // ---- AUDIT LOG ----
    [Route("admin/nhat-ky")]
    public async Task<IActionResult> AuditLogs(string? q)
    {
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.UserEmail.Contains(q) || x.Action.Contains(q) || x.Entity.Contains(q) || x.Details.Contains(q));
        ViewBag.Search = q;
        return View(await query.OrderByDescending(x => x.CreatedAt).Take(300).ToListAsync());
    }

    // ---- CATEGORIES & BRANDS ----
    [Route("admin/danh-muc")]
    public async Task<IActionResult> Categories() => View(await _db.Categories.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToListAsync());

    [HttpPost, Route("admin/danh-muc/luu")]
    public async Task<IActionResult> SaveCategory(int id, string name, string? icon, int displayOrder = 0, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name)) { TempData["Error"] = "Tên danh mục không được trống"; return RedirectToAction(nameof(Categories)); }
        Category? item = id > 0 ? await _db.Categories.FindAsync(id) : null;
        if (id > 0 && item == null) return NotFound();
        if (item == null) { item = new Category(); _db.Categories.Add(item); }
        var normalizedName = name.Trim();
        if (!string.Equals(item.Name, normalizedName, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(item.Slug)) item.Slug = Slugify(normalizedName);
        item.Name = normalizedName; item.Icon = string.IsNullOrWhiteSpace(icon) ? "📦" : icon.Trim();
        item.DisplayOrder = Math.Clamp(displayOrder, 0, 9999); item.IsActive = isActive;
        await _db.SaveChangesAsync(); await _audit.LogAsync(CurrentEmail(), "Lưu danh mục", "Category", item.Id.ToString(), item.Name);
        TempData["Success"] = "Đã lưu danh mục"; return RedirectToAction(nameof(Categories));
    }

    [HttpPost, Route("admin/danh-muc/{id}/an-hien")]
    public async Task<IActionResult> ToggleCategory(int id)
    {
        var item = await _db.Categories.FindAsync(id); if (item == null) return NotFound();
        item.IsActive = !item.IsActive; await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentEmail(), item.IsActive ? "Hiện danh mục" : "Ẩn danh mục", "Category", id.ToString(), item.Name);
        TempData["Success"] = item.IsActive ? "Đã hiện danh mục" : "Đã ẩn danh mục"; return RedirectToAction(nameof(Categories));
    }

    [Route("admin/thuong-hieu")]
    public async Task<IActionResult> Brands() => View(await _db.Brands.OrderBy(x => x.Name).ToListAsync());

    [HttpPost, Route("admin/thuong-hieu/luu")]
    public async Task<IActionResult> SaveBrand(int id, string name, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name)) { TempData["Error"] = "Tên thương hiệu không được trống"; return RedirectToAction(nameof(Brands)); }
        Brand? item = id > 0 ? await _db.Brands.FindAsync(id) : null;
        if (id > 0 && item == null) return NotFound();
        if (item == null) { item = new Brand(); _db.Brands.Add(item); }
        var normalizedName = name.Trim();
        if (!string.Equals(item.Name, normalizedName, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(item.Slug)) item.Slug = Slugify(normalizedName);
        item.Name = normalizedName; item.IsActive = isActive;
        await _db.SaveChangesAsync(); await _audit.LogAsync(CurrentEmail(), "Lưu thương hiệu", "Brand", item.Id.ToString(), item.Name);
        TempData["Success"] = "Đã lưu thương hiệu"; return RedirectToAction(nameof(Brands));
    }

    [HttpPost, Route("admin/thuong-hieu/{id}/an-hien")]
    public async Task<IActionResult> ToggleBrand(int id)
    {
        var item = await _db.Brands.FindAsync(id); if (item == null) return NotFound();
        item.IsActive = !item.IsActive; await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentEmail(), item.IsActive ? "Hiện thương hiệu" : "Ẩn thương hiệu", "Brand", id.ToString(), item.Name);
        TempData["Success"] = item.IsActive ? "Đã hiện thương hiệu" : "Đã ẩn thương hiệu"; return RedirectToAction(nameof(Brands));
    }

    // ---- TAGS ----
    [Route("admin/the-san-pham")]
    public async Task<IActionResult> Tags() => View(await _db.Tags.OrderBy(x => x.Name).ToListAsync());

    [HttpPost, Route("admin/the-san-pham/luu")]
    public async Task<IActionResult> SaveTag(int id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        { TempData["Error"] = "Tên thẻ không được để trống"; return RedirectToAction(nameof(Tags)); }
        Tag? item = id > 0 ? await _db.Tags.FindAsync(id) : null;
        if (id > 0 && item == null) return NotFound();
        var normalizedName = name.Trim();
        if (await _db.Tags.AnyAsync(t => t.Id != id && t.Name.ToLower() == normalizedName.ToLower()))
        { TempData["Error"] = "Thẻ này đã tồn tại"; return RedirectToAction(nameof(Tags)); }
        if (item == null) { item = new Tag(); _db.Tags.Add(item); }
        if (!string.Equals(item.Name, normalizedName, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(item.Slug))
            item.Slug = Slugify(normalizedName);
        item.Name = normalizedName;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentEmail(), "Lưu thẻ sản phẩm", "Tag", item.Id.ToString(), item.Name);
        TempData["Success"] = "Đã lưu thẻ sản phẩm";
        return RedirectToAction(nameof(Tags));
    }

    [HttpPost, Route("admin/the-san-pham/{id}/xoa")]
    public async Task<IActionResult> DeleteTag(int id)
    {
        var item = await _db.Tags.FindAsync(id);
        if (item == null) return NotFound();
        _db.Tags.Remove(item);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentEmail(), "Xóa thẻ sản phẩm", "Tag", id.ToString(), item.Name);
        TempData["Success"] = "Đã xóa thẻ. Nhãn dạng văn bản trên sản phẩm hiện có không bị thay đổi.";
        return RedirectToAction(nameof(Tags));
    }

    // ---- INVENTORY ----
    [Route("admin/ton-kho")]
    public async Task<IActionResult> Inventory(string? q, bool lowOnly = false)
    {
        var query = _db.Products.AsNoTracking().Include(p => p.Category).Include(p => p.Brand).Include(p => p.Images).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(p => p.Name.Contains(q) || p.Slug.Contains(q));
        if (lowOnly) query = query.Where(p => p.Stock <= 5);
        ViewBag.Search = q; ViewBag.LowOnly = lowOnly;
        return View(await query.OrderBy(p => p.Stock).ThenBy(p => p.Name).Take(300).ToListAsync());
    }

    [HttpPost, Route("admin/ton-kho/{id}/dieu-chinh")]
    public async Task<IActionResult> AdjustStock(int id, int delta, string? note)
    {
        if (delta == 0 || Math.Abs((long)delta) > 100000) { TempData["Error"] = "Số lượng điều chỉnh không hợp lệ"; return RedirectToAction(nameof(Inventory)); }
        var p = await _db.Products.FindAsync(id); if (p == null) return NotFound();
        if (p.Stock + delta < 0 || p.Stock + delta > 1_000_000) { TempData["Error"] = "Tồn kho mới không hợp lệ"; return RedirectToAction(nameof(Inventory)); }
        var before = p.Stock; p.Stock += delta; p.UpdatedAt = DateTime.Now; await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentEmail(), "Điều chỉnh tồn kho", "Product", id.ToString(), $"{p.Name}: {before} → {p.Stock}. {note}");
        TempData["Success"] = $"Đã cập nhật tồn kho của {p.Name}: {before} → {p.Stock}"; return RedirectToAction(nameof(Inventory));
    }

    // ---- BANNERS ----
    [Route("admin/banner")]
    public async Task<IActionResult> Banners() => View(await _db.Banners.OrderBy(x => x.SortOrder).ThenByDescending(x => x.CreatedAt).ToListAsync());

    [HttpPost, Route("admin/banner/luu")]
    public async Task<IActionResult> SaveBanner(int id, string title, string imageUrl, string? linkUrl, string position = "home-top", int sortOrder = 0, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(imageUrl)) { TempData["Error"] = "Tiêu đề và ảnh banner là bắt buộc"; return RedirectToAction(nameof(Banners)); }
        Banner? item = id > 0 ? await _db.Banners.FindAsync(id) : null;
        if (id > 0 && item == null) return NotFound();
        if (item == null) { item = new Banner(); _db.Banners.Add(item); }
        item.Title = title.Trim(); item.ImageUrl = imageUrl.Trim(); item.LinkUrl = linkUrl?.Trim();
        item.Position = string.IsNullOrWhiteSpace(position) ? "home-top" : position.Trim(); item.SortOrder = Math.Clamp(sortOrder, 0, 9999); item.IsActive = isActive;
        await _db.SaveChangesAsync(); await _audit.LogAsync(CurrentEmail(), "Lưu banner", "Banner", item.Id.ToString(), item.Title);
        TempData["Success"] = "Đã lưu banner"; return RedirectToAction(nameof(Banners));
    }

    [HttpPost, Route("admin/banner/{id}/an-hien")]
    public async Task<IActionResult> ToggleBanner(int id)
    {
        var item = await _db.Banners.FindAsync(id); if (item == null) return NotFound();
        item.IsActive = !item.IsActive; await _db.SaveChangesAsync(); await _audit.LogAsync(CurrentEmail(), item.IsActive ? "Hiện banner" : "Ẩn banner", "Banner", id.ToString(), item.Title); TempData["Success"] = "Đã cập nhật banner";
        return RedirectToAction(nameof(Banners));
    }

    [HttpPost, Route("admin/danh-gia/{id}/duyet")]
    public async Task<IActionResult> ApproveReview(int id)
    {
        var review = await _db.Reviews.FindAsync(id); if (review == null) return NotFound();
        review.Status = ReviewStatus.Approved; await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentEmail(), "Duyệt đánh giá", "Review", id.ToString(), review.Title);
        TempData["Success"] = "Đã duyệt đánh giá"; return RedirectToAction(nameof(Reviews));
    }

    [HttpPost, Route("admin/danh-gia/{id}/tu-choi")]
    public async Task<IActionResult> RejectReview(int id)
    {
        var review = await _db.Reviews.FindAsync(id); if (review == null) return NotFound();
        review.Status = ReviewStatus.Rejected; await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentEmail(), "Từ chối đánh giá", "Review", id.ToString(), review.Title);
        TempData["Success"] = "Đã từ chối đánh giá"; return RedirectToAction(nameof(Reviews));
    }

    [HttpPost, Route("admin/tin-nhan/{id}/trang-thai")]
    public async Task<IActionResult> UpdateMessageStatus(int id, ContactStatus status, string? staffNote)
    {
        var message = await _db.ContactMessages.FindAsync(id); if (message == null) return NotFound();
        message.Status = status; message.StaffNote = staffNote?.Trim(); message.HandledBy = CurrentEmail();
        message.IsRead = true; message.ResolvedAt = status == ContactStatus.Resolved ? DateTime.Now : null;
        await _db.SaveChangesAsync(); await _audit.LogAsync(CurrentEmail(), "Cập nhật tin nhắn", "ContactMessage", id.ToString(), status.Label());
        TempData["Success"] = "Đã cập nhật trạng thái tin nhắn"; return RedirectToAction(nameof(Messages));
    }

    [HttpPost, Route("admin/don-hang/{id}/van-don")]
    public async Task<IActionResult> UpdateTracking(int id, string trackingNumber)
    {
        var result = await _orders.SetTrackingAsync(id, trackingNumber ?? "", CurrentEmail());
        TempData[result.ok ? "Success" : "Error"] = result.msg;
        await _audit.LogAsync(CurrentEmail(), "Cập nhật mã vận đơn", "Order", id.ToString(), trackingNumber ?? "");
        return RedirectToAction(nameof(OrderDetail), new { id });
    }

    [HttpPost, Route("admin/nguoi-dung/{id}/quyen-thanh-toan")]
    public async Task<IActionResult> SetPaymentPermission(int id, bool canVerifyPayments)
    {
        var user = await _db.Users.FindAsync(id); if (user == null) return NotFound();
        if (user.Role != "Staff") { TempData["Error"] = "Chỉ có thể cấp quyền xác minh thanh toán cho nhân viên"; return RedirectToAction(nameof(Users)); }
        user.CanVerifyPayments = canVerifyPayments; await _db.SaveChangesAsync();
        await _audit.LogAsync(CurrentEmail(), "Đổi quyền xác minh thanh toán", "User", id.ToString(), $"{user.Email}: {canVerifyPayments}");
        TempData["Success"] = "Đã cập nhật quyền xác minh thanh toán"; return RedirectToAction(nameof(Users));
    }

    private string CurrentEmail() => User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name ?? "system";

    private static string CsvCell(string? value)
    {
        value ??= "";
        return "\"" + value.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";
    }

    private static string Slugify(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Guid.NewGuid().ToString("N")[..8];
        var s = text.ToLower().Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var c in s)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
            sb.Append(char.IsLetterOrDigit(c) ? c : '-');
        }
        var slug = System.Text.RegularExpressions.Regex.Replace(sb.ToString(), "-+", "-").Trim('-');
        return slug + "-" + Guid.NewGuid().ToString("N")[..4];
    }
}


