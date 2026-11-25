using GIADUNGCHATLUONG.DuLieu;
using GIADUNGCHATLUONG.TienIch;
using GIADUNGCHATLUONG.MoHinh;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TienIchHelper = GIADUNGCHATLUONG.TienIch.TienIch;

namespace GIADUNGCHATLUONG.DieuKhien;

/// <summary>Tiếp nhận yêu cầu xem, đặt, hủy và theo dõi đơn hàng của khách.</summary>
[Authorize]
public class DonHangController : Controller
{
    private readonly AppDb _db;
    public DonHangController(AppDb coSoDuLieu) => _db = coSoDuLieu;

    [HttpGet, Route("thanh-toan")]
    public IActionResult ThanhToan()
    {
        var list = GioHangHelper.DanhSach(HttpContext.Session, _db);
        if (list.Count == 0)
        {
            TempData["Loi"] = "Giỏ hàng trống. Vui lòng chọn sản phẩm trước khi thanh toán.";
            return RedirectToAction("Index", "GioHang");
        }
        if (list.Any(x => x.SanPham.TonKho <= 0 || x.SoLuong > x.SanPham.TonKho))
        {
            TempData["Loi"] = "Một sản phẩm trong giỏ không đủ tồn kho. Vui lòng cập nhật giỏ hàng trước khi thanh toán.";
            return RedirectToAction("Index", "GioHang");
        }

        var uid = TienIchHelper.LayIdNguoiDung(User);
        var user = uid.HasValue ? _db.NguoiDungs.Find(uid.Value) : null;
        var input = new ThanhToanInput
        {
            HoTen = user?.HoTen ?? "",
            DienThoai = user?.DienThoai ?? "",
            PhuongThuc = PhuongThucThanhToan.COD
        };
        GanDuLieuThanhToan(list);
        return View(input);
    }

    [HttpPost, Route("thanh-toan")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ThanhToan(ThanhToanInput input)
    {
        var uid = TienIchHelper.LayIdNguoiDung(User);
        if (uid == null) return RedirectToAction("DangNhap", "TaiKhoan");

        // Trim trước khi kiểm tra để chuỗi chỉ chứa dấu cách không thể vượt qua Required.
        input.HoTen = (input.HoTen ?? "").Trim();
        input.DienThoai = (input.DienThoai ?? "").Trim();
        input.DiaChi = (input.DiaChi ?? "").Trim();
        input.GhiChu = (input.GhiChu ?? "").Trim();
        input.PhuongThuc = (input.PhuongThuc ?? "").Trim();

        if (string.IsNullOrWhiteSpace(input.GhiChu))
            ModelState.AddModelError(nameof(input.GhiChu), "Ghi chú là bắt buộc. Anh/chị có thể nhập 'Không có' nếu không có yêu cầu thêm.");

        var soChuSoDienThoai = input.DienThoai.Count(kyTu => char.IsDigit(kyTu));
        if (soChuSoDienThoai is < 9 or > 12)
            ModelState.AddModelError(nameof(input.DienThoai), "Vui lòng nhập số điện thoại có từ 9 đến 12 chữ số.");

        if (!ModelState.IsValid)
        {
            var gioHienTai = GioHangHelper.DanhSach(HttpContext.Session, _db);
            if (gioHienTai.Count == 0)
            {
                TempData["Loi"] = "Giỏ hàng đã trống. Vui lòng chọn sản phẩm trước khi thanh toán.";
                return RedirectToAction("Index", "GioHang");
            }
            GanDuLieuThanhToan(gioHienTai);
            return View(input);
        }

        var gio = GioHangHelper.Lay(HttpContext.Session)
            .Where(x => x.Key > 0 && x.Value > 0)
            .ToDictionary(x => x.Key, x => x.Value);
        if (gio.Count == 0)
        {
            TempData["Loi"] = "Giỏ hàng trống. Vui lòng chọn sản phẩm trước khi thanh toán.";
            return RedirectToAction("Index", "GioHang");
        }

        // Đọc lại giá và tồn kho trong transaction; không tin giá trị từ form/trình duyệt.
        await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var ids = gio.Keys.ToList();
        var sanPhams = await _db.SanPhams
            .Where(x => ids.Contains(x.Id) && x.DangBan)
            .ToDictionaryAsync(x => x.Id);

        foreach (var item in gio)
        {
            if (!sanPhams.TryGetValue(item.Key, out var sp))
            {
                await transaction.RollbackAsync();
                TempData["Loi"] = "Một sản phẩm trong giỏ không còn kinh doanh. Vui lòng kiểm tra lại giỏ hàng.";
                return RedirectToAction("Index", "GioHang");
            }
            if (item.Value > sp.TonKho)
            {
                await transaction.RollbackAsync();
                TempData["Loi"] = $"'{sp.Ten}' hiện chỉ còn {sp.TonKho} sản phẩm. Vui lòng cập nhật giỏ hàng.";
                return RedirectToAction("Index", "GioHang");
            }
        }

        string maDon;
        do
        {
            maDon = "DH" + DateTime.Now.ToString("yyMMdd") + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        }
        while (await _db.DonHangs.AnyAsync(x => x.MaDon == maDon));

        var don = new DonHang
        {
            MaDon = maDon,
            NguoiDungId = uid.Value,
            HoTen = input.HoTen,
            DienThoai = input.DienThoai,
            DiaChi = input.DiaChi,
            GhiChu = input.GhiChu,
            PhuongThucThanhToan = input.PhuongThuc,
            TrangThai = TrangThaiDon.ChoXacNhan,
            ThanhToan = global::GIADUNGCHATLUONG.MoHinh.ThanhToan.ChuaThanhToan
        };

        foreach (var item in gio)
        {
            var sp = sanPhams[item.Key];
            sp.TonKho -= item.Value;
            sp.DaBan += item.Value;
            var donGia = sp.GiaHienTai;
            don.ChiTiet.Add(new ChiTietDonHang
            {
                SanPhamId = sp.Id,
                TenSanPham = sp.Ten,
                AnhSanPham = sp.AnhChinh,
                DonGia = donGia,
                SoLuong = item.Value
            });
            don.TongTien += donGia * item.Value;
        }

        _db.DonHangs.Add(don);
        try
        {
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            _db.ChangeTracker.Clear();
            ModelState.AddModelError(string.Empty, "Chưa thể lưu đơn hàng. Dữ liệu có thể vừa thay đổi, anh/chị vui lòng kiểm tra giỏ hàng và thử lại.");
            var gioHienTai = GioHangHelper.DanhSach(HttpContext.Session, _db);
            GanDuLieuThanhToan(gioHienTai);
            return View(input);
        }

        // Chỉ xóa giỏ sau khi đơn hàng đã được ghi và transaction đã commit.
        GioHangHelper.XoaHet(HttpContext.Session);
        TempData["DonHangThanhCong"] = "true";
        return RedirectToAction(nameof(ChiTiet), new { maDon });
    }

    [Route("don-hang-cua-toi")]
    public async Task<IActionResult> CuaToi()
    {
        var uid = TienIchHelper.LayIdNguoiDung(User);
        if (uid == null) return RedirectToAction("DangNhap", "TaiKhoan");
        var list = await _db.DonHangs.Include(x => x.ChiTiet)
            .Where(x => x.NguoiDungId == uid)
            .OrderByDescending(x => x.NgayTao).ToListAsync();
        return View(list);
    }

    [Route("don-hang/{maDon}")]
    public async Task<IActionResult> ChiTiet(string maDon)
    {
        var uid = TienIchHelper.LayIdNguoiDung(User);
        var don = await _db.DonHangs.Include(x => x.ChiTiet).FirstOrDefaultAsync(x => x.MaDon == maDon);
        if (don == null) return NotFound();
        if (don.NguoiDungId != uid && !TienIchHelper.LaQuanTri(User)) return NotFound();
        ViewBag.CauHinh = _db.CauHinhs.FirstOrDefault();
        ViewBag.DatHangThanhCong = TempData["DonHangThanhCong"] != null;
        return View(don);
    }

    private void GanDuLieuThanhToan(List<DongGioHang> list)
    {
        ViewBag.GioHang = list;
        ViewBag.CauHinh = _db.CauHinhs.FirstOrDefault();
        ViewBag.TongTien = list.Sum(x => x.ThanhTien);
    }
}
