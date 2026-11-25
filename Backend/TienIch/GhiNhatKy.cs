using System.Security.Claims;
using GIADUNGCHATLUONG.DuLieu;
using GIADUNGCHATLUONG.MoHinh;
using Microsoft.EntityFrameworkCore;

namespace GIADUNGCHATLUONG.TienIch;

/// <summary>Ghi lại hoạt động và yêu cầu của người dùng để quản trị viên tra cứu.</summary>
/// <summary>
/// Ghi hai tầng nhật ký:
/// - Nhật ký chi tiết: từng yêu cầu HTTP để tra cứu kỹ thuật.
/// - Nhật ký nghiệp vụ: chỉ các hành động quan trọng, không ghi lượt mở trang thông thường.
/// Không đọc/lưu body, mật khẩu, token, cookie hay thông tin thanh toán.
/// </summary>
public sealed class NhatKyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;

    public NhatKyMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _scopeFactory = scopeFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        if (LaTaiNguyenTinh(path) || LaTrangTraCuuNhatKy(path))
        {
            await _next(context);
            return;
        }

        try
        {
            await _next(context);
        }
        catch
        {
            await GhiAsync(context, path, true);
            throw;
        }

        await GhiAsync(context, path, false);
    }

    private static bool LaTaiNguyenTinh(string path)
        => path.StartsWith("/CSS/", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/JS/", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/HinhAnh/", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/TaiLen/", StringComparison.OrdinalIgnoreCase)
        || path.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase);

    // Không tự ghi lại thao tác đọc nhật ký, tránh tạo nhiễu và tránh vòng lặp khi tra cứu.
    private static bool LaTrangTraCuuNhatKy(string path)
        => path.Equals("/quan-tri/nhat-ky", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/quan-tri/nhat-ky-chi-tiet", StringComparison.OrdinalIgnoreCase)
        || path.Equals("/quan-tri/nhat-ky-theo-nguoi", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/api/quan-tri/nhat-ky/", StringComparison.OrdinalIgnoreCase);

    private async Task GhiAsync(HttpContext http, string path, bool exception)
    {
        try
        {
            var controller = http.Request.RouteValues.TryGetValue("controller", out var c) ? c?.ToString() : null;
            var action = http.Request.RouteValues.TryGetValue("action", out var a) ? a?.ToString() : null;
            controller ??= "HeThong";
            action ??= "KhongTimThay";

            var principal = http.User;
            var idRaw = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var id = int.TryParse(idRaw, out var userId) ? userId : (int?)null;
            var role = principal.FindFirstValue(ClaimTypes.Role) ?? (id.HasValue ? "" : "Khach");
            var actor = principal.Identity?.IsAuthenticated == true
                ? principal.Identity.Name ?? "Tài khoản"
                : "Khách vãng lai";

            var status = exception ? StatusCodes.Status500InternalServerError : http.Response.StatusCode;
            var redirect = http.Response.Headers["Location"].ToString();
            var result = KetQua(status, exception, redirect);
            var method = http.Request.Method.ToUpperInvariant();
            var title = TenRequest(controller, action, method);
            var detail = $"{controller}.{action} · {method} {path} · HTTP {status}";

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();

            // Luôn ghi yêu cầu động vào bảng chi tiết. Tài nguyên tĩnh đã bị loại ở trên.
            db.NhatKyChiTiets.Add(new NhatKyChiTiet
            {
                ThoiGian = DateTime.Now,
                NguoiDung = Cat(actor, 120),
                NguoiDungId = id,
                VaiTro = Cat(role, 32),
                HanhDong = Cat(title, 160),
                PhuongThuc = Cat(method, 12),
                DuongDan = Cat(path, 240),
                KetQua = result,
                MaTrangThai = status,
                DiaChiIP = Cat(http.Connection.RemoteIpAddress?.ToString() ?? "", 64),
                TraceId = Cat(http.TraceIdentifier, 80),
                ChiTiet = Cat(detail, 500)
            });

            // Các lần đăng nhập/đăng ký/đăng xuất đã được ghi ở controller để nhận đúng tài khoản.
            // Không lặp bản ghi nghiệp vụ ở middleware.
            var laXuLyXacThuc = controller.Equals("TaiKhoan", StringComparison.OrdinalIgnoreCase)
                && method == "POST" && (action is "DangNhap" or "DangXuat" or "DangKy" or "DoiMatKhau");

            if (!laXuLyXacThuc && LaHanhDongLon(controller, action, method, out var nhan, out var loai))
            {
                db.NhatKys.Add(new NhatKy
                {
                    ThoiGian = DateTime.Now,
                    NguoiDung = Cat(actor, 120),
                    NguoiDungId = id,
                    VaiTro = Cat(role, 32),
                    HanhDong = Cat(nhan, 120),
                    Loai = Cat(loai, 32),
                    KetQua = result,
                    ChiTiet = Cat(detail, 500),
                    DiaChiIP = Cat(http.Connection.RemoteIpAddress?.ToString() ?? "", 64),
                    DuongDan = Cat(path, 240),
                    TraceId = Cat(http.TraceIdentifier, 80)
                });
            }

            await db.SaveChangesAsync();
        }
        catch
        {
            // Nhật ký lỗi không được phép làm hỏng luồng mua sắm/quản trị.
        }
    }

    private static bool LaHanhDongLon(string controller, string action, string method, out string ten, out string loai)
    {
        ten = "";
        loai = "";
        if (method != "POST") return false;

        var key = $"{controller}.{action}";
        var map = new Dictionary<string, (string Ten, string Loai)>(StringComparer.OrdinalIgnoreCase)
        {
            ["GioHang.Them"] = ("Thay đổi giỏ hàng", "Mua sắm"),
            ["GioHang.CapNhat"] = ("Thay đổi giỏ hàng", "Mua sắm"),
            ["GioHang.Xoa"] = ("Thay đổi giỏ hàng", "Mua sắm"),
            ["GioHang.XoaHet"] = ("Thay đổi giỏ hàng", "Mua sắm"),
            ["DonHang.ThanhToan"] = ("Đặt hàng / thanh toán", "Mua sắm"),
            ["SanPham.DanhGia"] = ("Gửi đánh giá sản phẩm", "Khách hàng"),
            ["SanPham.YeuThich"] = ("Cập nhật danh sách yêu thích", "Khách hàng"),
            ["TrangChu.LienHe"] = ("Gửi yêu cầu liên hệ", "Khách hàng"),
            ["QuanTri.LuuSanPham"] = ("Thêm / chỉnh sửa sản phẩm", "Quản trị"),
            ["QuanTri.XoaSanPham"] = ("Xóa sản phẩm", "Quản trị"),
            ["QuanTri.HienSanPham"] = ("Ẩn / hiện sản phẩm", "Quản trị"),
            ["QuanTri.LuuDanhMuc"] = ("Thêm / chỉnh sửa danh mục", "Quản trị"),
            ["QuanTri.XoaDanhMuc"] = ("Xóa danh mục", "Quản trị"),
            ["QuanTri.LuuBanner"] = ("Thêm / chỉnh sửa banner", "Giao diện"),
            ["QuanTri.XoaBanner"] = ("Xóa banner", "Giao diện"),
            ["QuanTri.LuuBaiViet"] = ("Thêm / chỉnh sửa bài viết", "Nội dung"),
            ["QuanTri.XoaBaiViet"] = ("Xóa bài viết", "Nội dung"),
            ["QuanTri.DoiTrangThaiBaiViet"] = ("Thay đổi trạng thái bài viết", "Nội dung"),
            ["QuanTri.CapNhatLienHe"] = ("Cập nhật xử lý liên hệ", "Chăm sóc khách hàng"),
            ["QuanTri.DoiTrangThaiDon"] = ("Cập nhật trạng thái đơn hàng", "Đơn hàng"),
            ["QuanTri.XacNhanThanhToan"] = ("Xác nhận thanh toán đơn hàng", "Đơn hàng"),
            ["QuanTri.DuyetDanhGia"] = ("Duyệt / ẩn đánh giá", "Nội dung"),
            ["QuanTri.XoaDanhGia"] = ("Xóa đánh giá", "Nội dung"),
            ["QuanTri.KhoaNguoiDung"] = ("Khóa / mở tài khoản", "Tài khoản & quyền"),
            ["QuanTri.DoiVaiTro"] = ("Thay đổi vai trò tài khoản", "Tài khoản & quyền"),
            ["QuanTri.DatLaiMatKhau"] = ("Đặt lại mật khẩu", "Tài khoản & quyền"),
            ["QuanTri.PhanQuyen"] = ("Thay đổi quyền truy cập", "Tài khoản & quyền"),
            ["QuanTri.ThemNhanVien"] = ("Tạo tài khoản nhân sự", "Tài khoản & quyền"),
            ["QuanTri.CauHinh"] = ("Thay đổi cấu hình website", "Giao diện")
        };

        if (map.TryGetValue(key, out var item))
        {
            ten = item.Ten;
            loai = item.Loai;
            return true;
        }

        // Hỗ trợ controller/action đổi tên về sau nhưng vẫn giữ nhóm an toàn cho đổi mật khẩu.
        if (action.Contains("MatKhau", StringComparison.OrdinalIgnoreCase))
        {
            ten = "Thay đổi mật khẩu";
            loai = "Tài khoản & bảo mật";
            return true;
        }
        return false;
    }

    private static string TenRequest(string controller, string action, string method)
    {
        var ten = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Index"] = "Mở trang chính", ["ChiTiet"] = "Mở trang chi tiết", ["Dashboard"] = "Mở bảng điều khiển",
            ["DangNhap"] = "Mở trang đăng nhập", ["DangKy"] = "Mở trang đăng ký", ["DoiMatKhau"] = "Xử lý đổi mật khẩu", ["NhatKy"] = "Mở nhật ký nghiệp vụ",
            ["NhatKyChiTiet"] = "Mở nhật ký chi tiết", ["CauHinh"] = "Mở cấu hình cửa hàng", ["ThongKe"] = "Mở báo cáo thống kê"
        };
        return ten.TryGetValue(action, out var value) ? value : $"{(method == "GET" ? "Xem" : "Gửi yêu cầu")}: {controller}.{action}";
    }

    private static string KetQua(int status, bool exception, string redirect)
        => exception || status >= 500 ? "Lỗi máy chủ"
        : status == 403 || (status == 302 && redirect.Contains("tu-choi", StringComparison.OrdinalIgnoreCase)) ? "Bị từ chối"
        : status == 401 || (status == 302 && redirect.Contains("dang-nhap", StringComparison.OrdinalIgnoreCase)) ? "Chưa xác thực"
        : status >= 400 ? "Yêu cầu lỗi"
        : status >= 300 && status < 400 ? "Thành công"
        : "Thành công";

    private static string Cat(string? value, int max)
        => new string((value ?? "").Where(c => c != '\r' && c != '\n' && !char.IsControl(c)).Take(max).ToArray());
}
