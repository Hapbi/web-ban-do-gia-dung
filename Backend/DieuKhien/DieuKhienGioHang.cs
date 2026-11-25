using GIADUNGCHATLUONG.DuLieu;
using GIADUNGCHATLUONG.TienIch;
using Microsoft.AspNetCore.Mvc;
using TienIchHelper = GIADUNGCHATLUONG.TienIch.TienIch;

namespace GIADUNGCHATLUONG.DieuKhien;

/// <summary>Xử lý các thao tác thêm, sửa số lượng và xóa sản phẩm trong giỏ hàng.</summary>
public class GioHangController : Controller
{
    private readonly AppDb _db;
    public GioHangController(AppDb coSoDuLieu) => _db = coSoDuLieu;

    [Route("gio-hang")]
    public IActionResult Index()
    {
        var list = GioHangHelper.DanhSach(HttpContext.Session, _db);
        ViewBag.CauHinh = _db.CauHinhs.FirstOrDefault();
        return View(list);
    }

    [HttpPost, Route("gio-hang/them")]
    [ValidateAntiForgeryToken]
    public IActionResult Them(int sanPhamId, int soLuong = 1, string? returnUrl = null, bool muaLuon = false)
    {
        var sp = _db.SanPhams.FirstOrDefault(x => x.Id == sanPhamId && x.DangBan);
        if (sp == null)
        {
            TempData["Loi"] = "Sản phẩm không tồn tại";
            return RedirectToAction(nameof(Index));
        }
        if (sp.TonKho <= 0)
        {
            TempData["Loi"] = "Sản phẩm đã hết hàng";
            return RedirectToAction(nameof(Index));
        }

        var hienTai = Math.Max(0, GioHangHelper.Lay(HttpContext.Session).GetValueOrDefault(sanPhamId));
        var yeuCau = (long)hienTai + Math.Max(1, soLuong);
        var moi = (int)Math.Min(yeuCau, sp.TonKho);
        if (yeuCau > sp.TonKho)
            TempData["Loi"] = $"Số lượng đã được điều chỉnh theo tồn kho. Hiện chỉ còn {sp.TonKho} sản phẩm.";
        else
            TempData["ThanhCong"] = "Đã thêm vào giỏ hàng";
        GioHangHelper.Dat(HttpContext.Session, sanPhamId, moi);
        TempData["ThanhCong"] = "Đã thêm vào giỏ hàng";

        if (muaLuon) return RedirectToAction("ThanhToan", "DonHang");
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Route("gio-hang/cap-nhat")]
    [ValidateAntiForgeryToken]
    public IActionResult CapNhat(int sanPhamId, int delta)
    {
        var sp = _db.SanPhams.FirstOrDefault(x => x.Id == sanPhamId && x.DangBan);
        if (sp == null) return Json(new { ok = false, thongBao = "Sản phẩm không tồn tại" });

        var gio = GioHangHelper.Lay(HttpContext.Session);
        var soLuongMoi = (long)gio.GetValueOrDefault(sanPhamId) + delta;
        if (sp.TonKho <= 0 || soLuongMoi <= 0)
            gio.Remove(sanPhamId);
        else
            gio[sanPhamId] = (int)Math.Min(soLuongMoi, sp.TonKho);
        GioHangHelper.Luu(HttpContext.Session, gio);

        var list = GioHangHelper.DanhSach(HttpContext.Session, _db);
        var tongTien = list.Sum(x => x.ThanhTien);
        var tongSoLuong = list.Sum(x => x.SoLuong);
        var soLuongCuaSp = gio.GetValueOrDefault(sanPhamId);

        return Json(new {
            ok = true, sanPhamId, soLuong = soLuongCuaSp,
            thanhTien = (soLuongCuaSp * sp.GiaHienTai),
            thanhTienFormat = TienIchHelper.Tien(soLuongCuaSp * sp.GiaHienTai),
            tongTien, tongTienFormat = TienIchHelper.Tien(tongTien),
            tongSoLuong, daXoa = soLuongCuaSp == 0
        });
    }

    [HttpPost, Route("gio-hang/xoa")]
    [ValidateAntiForgeryToken]
    public IActionResult Xoa(int sanPhamId)
    {
        GioHangHelper.Xoa(HttpContext.Session, sanPhamId);
        TempData["ThanhCong"] = "Đã xoá sản phẩm khỏi giỏ";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Route("gio-hang/xoa-het")]
    [ValidateAntiForgeryToken]
    public IActionResult XoaHet()
    {
        GioHangHelper.XoaHet(HttpContext.Session);
        TempData["ThanhCong"] = "Đã xoá toàn bộ giỏ hàng";
        return RedirectToAction(nameof(Index));
    }
}
