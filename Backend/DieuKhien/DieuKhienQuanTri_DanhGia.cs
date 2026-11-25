using GIADUNGCHATLUONG.TienIch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GIADUNGCHATLUONG.DieuKhien;

public partial class QuanTriController
{
    [Route("quan-tri/danh-gia")]
    public async Task<IActionResult> DanhGia(string? trangThai)
    {
        if (!QuyenHelper.CoQuyen(User, "danhgia")) return Forbid();
        trangThai ??= "cho-duyet";
        var query = _db.DanhGias
            .Include(x => x.SanPham)
            .Include(x => x.NguoiDung)
            .AsQueryable();
        if (trangThai == "cho-duyet") query = query.Where(x => !x.DaDuyet);
        else if (trangThai == "da-duyet") query = query.Where(x => x.DaDuyet);
        ViewBag.TrangThai = trangThai ?? "cho-duyet";
        ViewBag.SoChoDuyet = await _db.DanhGias.CountAsync(x => !x.DaDuyet);
        return View(await query.OrderBy(x => x.DaDuyet).ThenByDescending(x => x.NgayTao).Take(300).ToListAsync());
    }

    [HttpPost, Route("quan-tri/danh-gia/{id:int}/duyet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DuyetDanhGia(int id, bool duyet = true)
    {
        if (!QuyenHelper.CoQuyen(User, "danhgia")) return Forbid();
        var dg = await _db.DanhGias.Include(x => x.SanPham).FirstOrDefaultAsync(x => x.Id == id);
        if (dg == null) return NotFound();
        dg.DaDuyet = duyet;
        await _db.SaveChangesAsync();
        TempData["ThanhCong"] = duyet ? "Đã duyệt đánh giá" : "Đã ẩn đánh giá";
        return RedirectToAction(nameof(DanhGia), new { trangThai = duyet ? "da-duyet" : "cho-duyet" });
    }

    [HttpPost, Route("quan-tri/danh-gia/{id:int}/xoa")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaDanhGia(int id)
    {
        if (!QuyenHelper.CoQuyen(User, "danhgia")) return Forbid();
        var dg = await _db.DanhGias.FindAsync(id);
        if (dg == null) return NotFound();
        _db.DanhGias.Remove(dg);
        await _db.SaveChangesAsync();
        TempData["ThanhCong"] = "Đã xóa đánh giá";
        return RedirectToAction(nameof(DanhGia), new { trangThai = "cho-duyet" });
    }
}
