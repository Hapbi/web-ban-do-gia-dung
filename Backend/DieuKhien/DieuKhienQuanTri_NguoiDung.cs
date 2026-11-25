using GIADUNGCHATLUONG.TienIch;
using GIADUNGCHATLUONG.MoHinh;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TienIchHelper = GIADUNGCHATLUONG.TienIch.TienIch;

namespace GIADUNGCHATLUONG.DieuKhien;

public partial class QuanTriController
{
    [Authorize(Roles = "Admin"), Route("quan-tri/nguoi-dung")]
    public async Task<IActionResult> NguoiDung(string? vaiTro, string? q)
    {
        var query = _db.NguoiDungs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(vaiTro)) query = query.Where(x => x.VaiTro == vaiTro);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.Email.Contains(q) || x.HoTen.Contains(q));
        ViewBag.VaiTro = vaiTro; ViewBag.Q = q;
        return View(await query.OrderBy(x => x.Id).ToListAsync());
    }

    [HttpPost, Authorize(Roles = "Admin"), Route("quan-tri/nguoi-dung/{id:int}/khoa")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KhoaNguoiDung(int id)
    {
        var u = await _db.NguoiDungs.FindAsync(id);
        if (u == null) return NotFound();
        if (TienIchHelper.LayIdNguoiDung(User) == id)
        {
            TempData["Loi"] = "Không thể khóa chính mình";
            return RedirectToAction(nameof(NguoiDung));
        }
        if (!u.BiKhoa && u.VaiTro == VaiTro.Admin
            && await _db.NguoiDungs.CountAsync(x => x.VaiTro == VaiTro.Admin && !x.BiKhoa) <= 1)
        {
            TempData["Loi"] = "Không thể khóa quản trị viên hoạt động cuối cùng.";
            return RedirectToAction(nameof(NguoiDung));
        }
        u.BiKhoa = !u.BiKhoa;
        await _db.SaveChangesAsync();
        TempData["ThanhCong"] = u.BiKhoa ? "Đã khóa tài khoản" : "Đã mở khóa";
        return RedirectToAction(nameof(NguoiDung));
    }

    [HttpPost, Authorize(Roles = "Admin"), Route("quan-tri/nguoi-dung/{id:int}/vai-tro")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiVaiTro(int id, string vaiTro)
    {
        var u = await _db.NguoiDungs.FindAsync(id);
        if (u == null) return NotFound();
        if (TienIchHelper.LayIdNguoiDung(User) == id && vaiTro != VaiTro.Admin)
        {
            TempData["Loi"] = "Không thể tự hạ quyền";
            return RedirectToAction(nameof(NguoiDung));
        }
        if (!new[] { VaiTro.Admin, VaiTro.AdminPhu, VaiTro.NhanVien, VaiTro.KhachHang }.Contains(vaiTro)) return BadRequest();
        if (vaiTro == VaiTro.Admin && u.VaiTro != VaiTro.Admin)
        {
            TempData["Loi"] = "Không thể cấp quyền Admin lớn từ màn hình này. Chỉ tài khoản Admin lớn hiện hữu mới giữ quyền toàn quyền.";
            return RedirectToAction(nameof(NguoiDung));
        }
        if (u.VaiTro == VaiTro.Admin && vaiTro != VaiTro.Admin && !u.BiKhoa
            && await _db.NguoiDungs.CountAsync(x => x.VaiTro == VaiTro.Admin && !x.BiKhoa) <= 1)
        {
            TempData["Loi"] = "Không thể hạ quyền quản trị viên hoạt động cuối cùng.";
            return RedirectToAction(nameof(NguoiDung));
        }
        u.VaiTro = vaiTro;
        if (vaiTro == VaiTro.KhachHang || vaiTro == VaiTro.Admin) u.Quyen = "";
        if (vaiTro == VaiTro.AdminPhu && string.IsNullOrWhiteSpace(u.Quyen)) u.Quyen = QuyenHelper.Luu(QuyenHelper.MacDinh);
        await _db.SaveChangesAsync();
        TempData["ThanhCong"] = "Đã đổi vai trò";
        return RedirectToAction(nameof(NguoiDung));
    }

    [HttpPost, Authorize(Roles = "Admin"), Route("quan-tri/nguoi-dung/{id:int}/dat-lai-mk")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DatLaiMatKhau(int id, string matKhauMoi)
    {
        var u = await _db.NguoiDungs.FindAsync(id);
        if (u == null) return NotFound();
        if (string.IsNullOrWhiteSpace(matKhauMoi) || matKhauMoi.Length < 8)
        {
            TempData["Loi"] = "Mật khẩu mới tối thiểu 8 ký tự";
            return RedirectToAction(nameof(NguoiDung));
        }
        u.MatKhauHash = MatKhauHelper.Hash(matKhauMoi);
        await _db.SaveChangesAsync();
        TempData["ThanhCong"] = $"Đã đặt lại mật khẩu cho {u.Email}";
        return RedirectToAction(nameof(NguoiDung));
    }

    [HttpGet, Authorize(Roles = "Admin"), Route("quan-tri/nguoi-dung/{id:int}/phan-quyen")]
    public async Task<IActionResult> PhanQuyen(int id)
    {
        var u = await _db.NguoiDungs.FindAsync(id);
        if (u == null) return NotFound();
        if (u.VaiTro is not (VaiTro.NhanVien or VaiTro.AdminPhu))
        {
            TempData["Loi"] = "Chỉ nhân viên hoặc Admin phụ mới cần phân quyền";
            return RedirectToAction(nameof(NguoiDung));
        }
        ViewBag.DaCap = QuyenHelper.HieuLuc(u.VaiTro, u.Quyen);
        return View(u);
    }

    [HttpPost, Authorize(Roles = "Admin"), Route("quan-tri/nguoi-dung/{id:int}/phan-quyen")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PhanQuyen(int id, string[] quyen, bool datLai = false)
    {
        var u = await _db.NguoiDungs.FindAsync(id);
        if (u == null || u.VaiTro is not (VaiTro.NhanVien or VaiTro.AdminPhu)) return NotFound();
        u.Quyen = datLai ? "" : QuyenHelper.Luu(quyen ?? Array.Empty<string>());
        await _db.SaveChangesAsync();
        TempData["ThanhCong"] = "Đã lưu phân quyền";
        return RedirectToAction(nameof(NguoiDung));
    }

    [HttpPost, Authorize(Roles = "Admin"), Route("quan-tri/nguoi-dung/them-nhan-vien")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ThemNhanVien(string hoTen, string email, string matKhau, string vaiTro = VaiTro.NhanVien, string[]? quyen = null)
    {
        if (string.IsNullOrWhiteSpace(hoTen) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(matKhau))
        {
            TempData["Loi"] = "Vui lòng nhập đủ thông tin";
            return RedirectToAction(nameof(NguoiDung));
        }
        if (matKhau.Length < 8) { TempData["Loi"] = "Mật khẩu tối thiểu 8 ký tự"; return RedirectToAction(nameof(NguoiDung)); }
        email = email.Trim().ToLower();
        if (await _db.NguoiDungs.AnyAsync(x => x.Email.ToLower() == email))
        {
            TempData["Loi"] = "Email đã tồn tại";
            return RedirectToAction(nameof(NguoiDung));
        }
        if (vaiTro is not (VaiTro.NhanVien or VaiTro.AdminPhu)) vaiTro = VaiTro.NhanVien;
        _db.NguoiDungs.Add(new NguoiDung
        {
            Email = email, HoTen = hoTen.Trim(), VaiTro = vaiTro,
            Quyen = QuyenHelper.Luu(quyen ?? Array.Empty<string>()),
            MatKhauHash = MatKhauHelper.Hash(matKhau)
        });
        await _db.SaveChangesAsync();
        TempData["ThanhCong"] = "Đã thêm nhân viên";
        return RedirectToAction(nameof(NguoiDung));
    }
}
