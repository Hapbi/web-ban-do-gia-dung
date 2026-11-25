using GIADUNGCHATLUONG.MoHinh;
using GIADUNGCHATLUONG.TienIch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GIADUNGCHATLUONG.DieuKhien;

public partial class QuanTriController
{
    [Route("quan-tri/don-hang")]
    public async Task<IActionResult> DonHang(string? trangThai, string? q)
    {
        if (!QuyenHelper.CoQuyen(User, "donhang")) return Forbid();
        var query = _db.DonHangs.Include(x => x.ChiTiet).AsQueryable();
        if (!string.IsNullOrWhiteSpace(trangThai)) query = query.Where(x => x.TrangThai == trangThai);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.MaDon.Contains(q) || x.HoTen.Contains(q) || x.DienThoai.Contains(q));
        ViewBag.TrangThai = trangThai;
        ViewBag.Q = q;
        return View(await query.OrderByDescending(x => x.NgayTao).Take(300).ToListAsync());
    }

    [HttpPost, Route("quan-tri/don-hang/{id:int}/trang-thai")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiTrangThaiDon(int id, string trangThai)
    {
        if (!QuyenHelper.CoQuyen(User, "donhang")) return Forbid();
        var don = await _db.DonHangs.Include(x => x.ChiTiet).FirstOrDefaultAsync(x => x.Id == id);
        if (don == null) return NotFound();
        if (!TrangThaiDon.TatCa.Contains(trangThai)) return BadRequest();
        if (don.TrangThai == trangThai)
            return RedirectToAction(nameof(DonHang));

        var chuyenTrangThaiHopLe = don.TrangThai switch
        {
            TrangThaiDon.ChoXacNhan => trangThai == TrangThaiDon.DaXacNhan || trangThai == TrangThaiDon.DaHuy,
            TrangThaiDon.DaXacNhan => trangThai == TrangThaiDon.DangGiao || trangThai == TrangThaiDon.DaHuy,
            TrangThaiDon.DangGiao => trangThai == TrangThaiDon.HoanThanh,
            _ => false
        };
        if (!chuyenTrangThaiHopLe)
        {
            TempData["Loi"] = "Luồng trạng thái không hợp lệ. Đơn đã hoàn thành hoặc đã hủy không thể mở lại.";
            return RedirectToAction(nameof(DonHang));
        }
        if (trangThai == TrangThaiDon.DaHuy)
        {
            if (don.ThanhToan == ThanhToan.DaThanhToan)
            {
                TempData["Loi"] = "Đơn đã thanh toán. Cần xử lý hoàn tiền trước khi hủy đơn.";
                return RedirectToAction(nameof(DonHang));
            }
            foreach (var ct in don.ChiTiet)
            {
                var sp = await _db.SanPhams.FindAsync(ct.SanPhamId);
                if (sp != null)
                {
                    sp.TonKho += ct.SoLuong;
                    sp.DaBan = Math.Max(0, sp.DaBan - ct.SoLuong);
                }
            }
        }
        don.TrangThai = trangThai;
        if (trangThai == TrangThaiDon.HoanThanh && don.PhuongThucThanhToan == PhuongThucThanhToan.COD)
            don.ThanhToan = ThanhToan.DaThanhToan;
        await _db.SaveChangesAsync();
        TempData["ThanhCong"] = "Đã cập nhật trạng thái";
        return RedirectToAction(nameof(DonHang));
    }


    [HttpPost, Route("quan-tri/don-hang/{id:int}/xac-nhan-thanh-toan")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XacNhanThanhToan(int id)
    {
        if (!QuyenHelper.CoQuyen(User, "donhang")) return Forbid();
        var don = await _db.DonHangs.FirstOrDefaultAsync(x => x.Id == id);
        if (don == null) return NotFound();
        if (don.TrangThai == TrangThaiDon.DaHuy)
        {
            TempData["Loi"] = "Không thể xác nhận thanh toán cho đơn đã hủy.";
            return RedirectToAction(nameof(DonHang));
        }
        if (don.PhuongThucThanhToan == PhuongThucThanhToan.COD && don.TrangThai != TrangThaiDon.HoanThanh)
        {
            TempData["Loi"] = "Đơn COD chỉ được xác nhận đã thu tiền sau khi giao hàng hoàn tất.";
            return RedirectToAction(nameof(DonHang));
        }
        don.ThanhToan = ThanhToan.DaThanhToan;
        await _db.SaveChangesAsync();
        TempData["ThanhCong"] = "Đã xác nhận thanh toán.";
        return RedirectToAction(nameof(DonHang));
    }
}
