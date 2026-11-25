using System.Security.Claims;
using GIADUNGCHATLUONG.DuLieu;
using GIADUNGCHATLUONG.TienIch;
using GIADUNGCHATLUONG.MoHinh;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TienIchHelper = GIADUNGCHATLUONG.TienIch.TienIch;

namespace GIADUNGCHATLUONG.DieuKhien;

/// <summary>Xử lý đăng ký, đăng nhập, đăng xuất và đổi mật khẩu.</summary>
public class TaiKhoanController : Controller
{
    private readonly AppDb _db;
    public TaiKhoanController(AppDb coSoDuLieu) => _db = coSoDuLieu;

    [Route("tai-khoan/dang-nhap")]
    public IActionResult DangNhap(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost, Route("tai-khoan/dang-nhap")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DangNhap(string email, string matKhau, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(matKhau))
        {
            TempData["Loi"] = "Vui lòng nhập email và mật khẩu";
            return View();
        }
        email = email.Trim().ToLowerInvariant();
        // Cho phép tài khoản quản trị dùng tên đăng nhập ngắn "admin" như cấu hình demo.
        if (email == "admin") email = "admin@shop.vn";
        var user = await _db.NguoiDungs.FirstOrDefaultAsync(x => x.Email.ToLower() == email);
        if (user == null || user.BiKhoa || !MatKhauHelper.KiemTra(matKhau, user.MatKhauHash))
        {
            await GhiNhatKyDangNhapAsync(null, "Đăng nhập thất bại", "Thông tin đăng nhập không hợp lệ", "Thất bại");
            TempData["Loi"] = "Email hoặc mật khẩu không đúng";
            return View();
        }

        if (MatKhauHelper.LaHashCu(user.MatKhauHash))
        {
            user.MatKhauHash = MatKhauHelper.Hash(matKhau);
            await _db.SaveChangesAsync();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.HoTen),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.VaiTro)
        };
        foreach (var q in QuyenHelper.HieuLuc(user.VaiTro, user.Quyen))
            claims.Add(new Claim("Quyen", q));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await GhiNhatKyDangNhapAsync(user, "Đăng nhập thành công", "Đăng nhập vào website", "Thành công");
        await HttpContext.SignInAsync(new ClaimsPrincipal(identity));

        // Lưu thông tin tài khoản vào Session để các trang có thể đọc nhanh trong phiên hiện tại.
        // Quyền truy cập vẫn do Cookie Authentication và [Authorize] kiểm soát.
        HttpContext.Session.SetInt32("NguoiDungId", user.Id);
        HttpContext.Session.SetString("HoTenNguoiDung", user.HoTen);

        TempData["ThanhCong"] = $"Xin chào {user.HoTen}!";

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return user.VaiTro == VaiTro.KhachHang
            ? RedirectToAction("Index", "TrangChu")
            : RedirectToAction("Dashboard", "QuanTri");
    }

    [HttpPost, Route("tai-khoan/dang-xuat")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DangXuat()
    {
        var id = TienIchHelper.LayIdNguoiDung(User);
        var user = id.HasValue ? await _db.NguoiDungs.FindAsync(id.Value) : null;
        await GhiNhatKyDangNhapAsync(user, "Đăng xuất", "Kết thúc phiên đăng nhập", "Thành công");
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "TrangChu");
    }

    [Route("tai-khoan/dang-ky")]
    public IActionResult DangKy() => View();

    [HttpPost, Route("tai-khoan/dang-ky")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DangKy(string hoTen, string email, string dienThoai, string matKhau, string xacNhanMatKhau)
    {
        if (string.IsNullOrWhiteSpace(hoTen) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(matKhau))
        {
            await GhiNhatKyDangNhapAsync(null, "Đăng ký thất bại", "Thiếu thông tin bắt buộc", "Thất bại");
            TempData["Loi"] = "Vui lòng nhập đầy đủ thông tin";
            return View();
        }
        if (matKhau.Length < 8)
        {
            await GhiNhatKyDangNhapAsync(null, "Đăng ký thất bại", "Mật khẩu không đạt độ dài tối thiểu", "Thất bại");
            TempData["Loi"] = "Mật khẩu tối thiểu 8 ký tự"; return View();
        }
        if (matKhau != xacNhanMatKhau)
        {
            await GhiNhatKyDangNhapAsync(null, "Đăng ký thất bại", "Mật khẩu xác nhận không khớp", "Thất bại");
            TempData["Loi"] = "Mật khẩu xác nhận không khớp"; return View();
        }

        email = email.Trim().ToLower();
        if (await _db.NguoiDungs.AnyAsync(x => x.Email.ToLower() == email))
        {
            await GhiNhatKyDangNhapAsync(null, "Đăng ký thất bại", "Email đăng ký đã tồn tại", "Thất bại");
            TempData["Loi"] = "Email đã tồn tại";
            return View();
        }

        var user = new NguoiDung
        {
            Email = email, HoTen = hoTen.Trim(),
            DienThoai = dienThoai?.Trim() ?? "",
            VaiTro = VaiTro.KhachHang,
            MatKhauHash = MatKhauHelper.Hash(matKhau)
        };
        _db.NguoiDungs.Add(user);
        await _db.SaveChangesAsync();
        await GhiNhatKyDangNhapAsync(user, "Đăng ký tài khoản", "Tạo tài khoản khách hàng mới", "Thành công");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.HoTen),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.VaiTro)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(new ClaimsPrincipal(identity));

        // Lưu thông tin tài khoản vào Session để các trang có thể đọc nhanh trong phiên hiện tại.
        // Quyền truy cập vẫn do Cookie Authentication và [Authorize] kiểm soát.
        HttpContext.Session.SetInt32("NguoiDungId", user.Id);
        HttpContext.Session.SetString("HoTenNguoiDung", user.HoTen);

        TempData["ThanhCong"] = "Đăng ký thành công!";
        return RedirectToAction("Index", "TrangChu");
    }

    [Authorize, Route("tai-khoan/doi-mat-khau")]
    public IActionResult DoiMatKhau()
    {
        ViewData["Title"] = "Đổi mật khẩu";
        return View();
    }

    [Authorize, HttpPost, Route("tai-khoan/doi-mat-khau")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiMatKhau(string matKhauHienTai, string matKhauMoi, string xacNhanMatKhau)
    {
        var id = TienIchHelper.LayIdNguoiDung(User);
        var user = id.HasValue ? await _db.NguoiDungs.FindAsync(id.Value) : null;
        if (user == null) return RedirectToAction(nameof(DangNhap));

        if (!MatKhauHelper.KiemTra(matKhauHienTai ?? "", user.MatKhauHash))
        {
            await GhiNhatKyDangNhapAsync(user, "Đổi mật khẩu thất bại", "Mật khẩu hiện tại không chính xác", "Thất bại");
            TempData["Loi"] = "Mật khẩu hiện tại chưa đúng.";
            return View();
        }
        if (string.IsNullOrWhiteSpace(matKhauMoi) || matKhauMoi.Length < 8)
        {
            await GhiNhatKyDangNhapAsync(user, "Đổi mật khẩu thất bại", "Mật khẩu mới không đạt tối thiểu 8 ký tự", "Thất bại");
            TempData["Loi"] = "Mật khẩu mới cần có ít nhất 8 ký tự.";
            return View();
        }
        if (!string.Equals(matKhauMoi, xacNhanMatKhau, StringComparison.Ordinal))
        {
            await GhiNhatKyDangNhapAsync(user, "Đổi mật khẩu thất bại", "Mật khẩu xác nhận không khớp", "Thất bại");
            TempData["Loi"] = "Mật khẩu xác nhận chưa khớp.";
            return View();
        }

        user.MatKhauHash = MatKhauHelper.Hash(matKhauMoi);
        await _db.SaveChangesAsync();
        await GhiNhatKyDangNhapAsync(user, "Đổi mật khẩu thành công", "Người dùng chủ động đổi mật khẩu", "Thành công");
        TempData["ThanhCong"] = "Đổi mật khẩu thành công.";
        return RedirectToAction(nameof(DoiMatKhau));
    }

    [Route("tai-khoan/tu-choi")]
    public IActionResult TuChoi() => Content("Bạn không có quyền truy cập trang này.");
    private async Task GhiNhatKyDangNhapAsync(NguoiDung? user, string hanhDong, string chiTiet, string ketQua)
    {
        _db.NhatKys.Add(new NhatKy
        {
            ThoiGian = DateTime.Now,
            NguoiDung = user?.HoTen ?? "Khách vãng lai",
            NguoiDungId = user?.Id,
            VaiTro = user?.VaiTro ?? "Khách",
            HanhDong = hanhDong,
            Loai = "Xác thực",
            KetQua = ketQua,
            ChiTiet = chiTiet,
            DiaChiIP = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
            DuongDan = HttpContext.Request.Path.Value ?? "",
            TraceId = HttpContext.TraceIdentifier
        });
        await _db.SaveChangesAsync();
    }
}
