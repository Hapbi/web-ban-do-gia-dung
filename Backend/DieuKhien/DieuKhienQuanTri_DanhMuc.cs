using GIADUNGCHATLUONG.DuLieu;
using GIADUNGCHATLUONG.TienIch;
using GIADUNGCHATLUONG.MoHinh;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GIADUNGCHATLUONG.DieuKhien;

public partial class QuanTriController
{
    [Route("quan-tri/danh-muc")]
    public async Task<IActionResult> DanhMuc()
    {
        if (!QuyenHelper.CoQuyen(User, "danhmuc")) return Forbid();
        return View(await _db.DanhMucs.Include(x => x.DanhSachSanPham).OrderBy(x => x.Id).ToListAsync());
    }

    [HttpPost, Route("quan-tri/danh-muc/luu")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LuuDanhMuc(int id, string ten, string icon)
    {
        if (!QuyenHelper.CoQuyen(User, "danhmuc")) return Forbid();
        if (string.IsNullOrWhiteSpace(ten))
        {
            TempData["Loi"] = "Tên danh mục không được trống";
            return RedirectToAction(nameof(DanhMuc));
        }
        DanhMuc dm;
        if (id == 0) { dm = new DanhMuc(); _db.DanhMucs.Add(dm); }
        else
        {
            dm = await _db.DanhMucs.FindAsync(id);
            if (dm == null) return NotFound();
        }
        dm.Ten = ten.Trim();
        dm.Icon = string.IsNullOrWhiteSpace(icon) ? "📦" : icon.Trim();
        await _db.SaveChangesAsync();
        TempData["ThanhCong"] = "Đã lưu danh mục";
        return RedirectToAction(nameof(DanhMuc));
    }

    [HttpPost, Route("quan-tri/danh-muc/{id:int}/xoa")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaDanhMuc(int id)
    {
        if (!QuyenHelper.CoQuyen(User, "danhmuc")) return Forbid();
        var dm = await _db.DanhMucs.Include(x => x.DanhSachSanPham).FirstOrDefaultAsync(x => x.Id == id);
        if (dm == null) return NotFound();
        if (dm.DanhSachSanPham.Any())
        {
            TempData["Loi"] = "Không xóa được: danh mục còn sản phẩm";
            return RedirectToAction(nameof(DanhMuc));
        }
        _db.DanhMucs.Remove(dm);
        await _db.SaveChangesAsync();
        TempData["ThanhCong"] = "Đã xóa danh mục";
        return RedirectToAction(nameof(DanhMuc));
    }

    [Route("quan-tri/banner")]
    public async Task<IActionResult> Banner()
    {
        if (!QuyenHelper.CoQuyen(User, "banner")) return Forbid();
        return View(await _db.Banners.OrderBy(x => x.ThuTu).ThenBy(x => x.Id).ToListAsync());
    }

    [HttpPost, Route("quan-tri/banner/luu")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LuuBanner(Banner input, IFormFile? anhFile)
    {
        if (!QuyenHelper.CoQuyen(User, "banner")) return Forbid();
        if (string.IsNullOrWhiteSpace(input.TieuDe))
        {
            TempData["Loi"] = "Vui lòng nhập tiêu đề banner.";
            return RedirectToAction(nameof(Banner));
        }
        Banner b;
        if (input.Id == 0) { b = new Banner(); _db.Banners.Add(b); }
        else
        {
            b = await _db.Banners.FindAsync(input.Id);
            if (b == null) return NotFound();
        }

        if (anhFile != null && anhFile.Length > 0)
        {
            var duoiTep = Path.GetExtension(anhFile.FileName).ToLowerInvariant();
            var dinhDangChoPhep = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            if (!dinhDangChoPhep.Contains(duoiTep) || anhFile.Length > 8 * 1024 * 1024)
            {
                TempData["Loi"] = "Ảnh banner phải là JPG, PNG, WEBP hoặc GIF và tối đa 8MB.";
                return RedirectToAction(nameof(Banner));
            }
            var duLieuDauTep = new byte[12];
            using (var luongDuLieu = anhFile.OpenReadStream()) _ = await luongDuLieu.ReadAsync(duLieuDauTep.AsMemory(0, duLieuDauTep.Length));
            var anhHopLe = duoiTep switch
            {
                ".jpg" or ".jpeg" => duLieuDauTep[0] == 0xFF && duLieuDauTep[1] == 0xD8 && duLieuDauTep[2] == 0xFF,
                ".png" => duLieuDauTep[0] == 0x89 && duLieuDauTep[1] == 0x50 && duLieuDauTep[2] == 0x4E && duLieuDauTep[3] == 0x47,
                ".gif" => System.Text.Encoding.ASCII.GetString(duLieuDauTep, 0, 3) == "GIF",
                ".webp" => System.Text.Encoding.ASCII.GetString(duLieuDauTep, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(duLieuDauTep, 8, 4) == "WEBP",
                _ => false
            };
            if (!anhHopLe)
            {
                TempData["Loi"] = "Tệp tải lên không phải ảnh hợp lệ.";
                return RedirectToAction(nameof(Banner));
            }
            var thuMuc = Path.Combine(_env.WebRootPath, "TaiLen", "banner");
            Directory.CreateDirectory(thuMuc);
            var tenTep = Guid.NewGuid().ToString("N") + duoiTep;
            using (var tepDauRa = System.IO.File.Create(Path.Combine(thuMuc, tenTep))) await anhFile.CopyToAsync(tepDauRa);
            b.AnhUrl = "/TaiLen/banner/" + tenTep;
        }
        else if (!string.IsNullOrWhiteSpace(input.AnhUrl))
        {
            var duongDan = input.AnhUrl.Trim();
            var laDuongDanNoiBo = duongDan.StartsWith("/", StringComparison.Ordinal) && !duongDan.StartsWith("//", StringComparison.Ordinal);
            var laDuongDanNgoai = Uri.TryCreate(duongDan, UriKind.Absolute, out var diaChiNgoai) && (diaChiNgoai.Scheme == Uri.UriSchemeHttp || diaChiNgoai.Scheme == Uri.UriSchemeHttps);
            if (!laDuongDanNoiBo && !laDuongDanNgoai)
            {
                TempData["Loi"] = "Link ảnh cần là đường dẫn nội bộ hoặc URL HTTP/HTTPS.";
                return RedirectToAction(nameof(Banner));
            }
            b.AnhUrl = duongDan;
        }
        else if (string.IsNullOrWhiteSpace(b.AnhUrl))
        {
            TempData["Loi"] = "Hãy tải ảnh lên hoặc nhập link ảnh.";
            return RedirectToAction(nameof(Banner));
        }

        var lienKet = (input.LienKet ?? "").Trim();
        if (string.IsNullOrWhiteSpace(lienKet)) lienKet = "/san-pham";
        var lienKetNoiBo = lienKet.StartsWith("/", StringComparison.Ordinal) && !lienKet.StartsWith("//", StringComparison.Ordinal);
        var lienKetNgoai = Uri.TryCreate(lienKet, UriKind.Absolute, out var uriLienKet)
            && (uriLienKet.Scheme == Uri.UriSchemeHttp || uriLienKet.Scheme == Uri.UriSchemeHttps);
        if (!lienKetNoiBo && !lienKetNgoai)
        {
            TempData["Loi"] = "Liên kết banner chỉ nhận đường dẫn nội bộ hoặc URL HTTP/HTTPS.";
            return RedirectToAction(nameof(Banner));
        }
        b.TieuDe = input.TieuDe.Trim();
        b.LienKet = lienKet;
        b.ThuTu = Math.Clamp(input.ThuTu, 0, 9999);
        b.DangHienThi = input.DangHienThi;
        await _db.SaveChangesAsync();
        TempData["ThanhCong"] = "Đã lưu banner. Banner được dùng trực tiếp ở trang chủ.";
        return RedirectToAction(nameof(Banner));
    }

    [HttpPost, Route("quan-tri/banner/{id:int}/xoa")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaBanner(int id)
    {
        if (!QuyenHelper.CoQuyen(User, "banner")) return Forbid();
        var b = await _db.Banners.FindAsync(id);
        if (b != null)
        {
            if (b.AnhUrl.StartsWith("/TaiLen/banner/", StringComparison.OrdinalIgnoreCase))
            {
                var tepAnh = Path.Combine(_env.WebRootPath, b.AnhUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(tepAnh)) System.IO.File.Delete(tepAnh);
            }
            _db.Banners.Remove(b);
            await _db.SaveChangesAsync();
            TempData["ThanhCong"] = "Đã xóa banner";
        }
        return RedirectToAction(nameof(Banner));
    }

    [Route("quan-tri/bai-viet")]
    public async Task<IActionResult> BaiViet()
    {
        if (!QuyenHelper.CoQuyen(User, "baiviet")) return Forbid();
        return View(await _db.BaiViets.OrderByDescending(x => x.NgayTao).ToListAsync());
    }

    [HttpGet, Route("quan-tri/bai-viet/sua/{id:int?}")]
    public async Task<IActionResult> SuaBaiViet(int? id)
    {
        if (!QuyenHelper.CoQuyen(User, "baiviet")) return Forbid();
        if (id == null) return View(new BaiViet { DaDang = true });
        var bv = await _db.BaiViets.FindAsync(id);
        return bv == null ? NotFound() : View(bv);
    }

    [HttpPost, Route("quan-tri/bai-viet/luu")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LuuBaiViet(BaiViet input)
    {
        if (!QuyenHelper.CoQuyen(User, "baiviet")) return Forbid();
        if (string.IsNullOrWhiteSpace(input.TieuDe))
        {
            TempData["Loi"] = "Vui lòng nhập tiêu đề";
            return View("SuaBaiViet", input);
        }
        BaiViet bv;
        if (input.Id == 0)
        {
            bv = new BaiViet { TacGia = User.Identity?.Name ?? "" };
            _db.BaiViets.Add(bv);
        }
        else
        {
            bv = await _db.BaiViets.FindAsync(input.Id);
            if (bv == null) return NotFound();
        }
        bv.TieuDe = input.TieuDe.Trim();
        bv.TomTat = input.TomTat?.Trim() ?? "";
        bv.NoiDung = input.NoiDung?.Trim() ?? "";
        bv.AnhBia = input.AnhBia?.Trim() ?? "";
        bv.DaDang = input.DaDang;
        await _db.SaveChangesAsync();
        TempData["ThanhCong"] = "Đã lưu bài viết";
        return RedirectToAction(nameof(BaiViet));
    }

    [HttpPost, Route("quan-tri/bai-viet/{id:int}/xoa")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaBaiViet(int id)
    {
        if (!QuyenHelper.CoQuyen(User, "baiviet")) return Forbid();
        var bv = await _db.BaiViets.FindAsync(id);
        if (bv != null)
        {
            _db.BaiViets.Remove(bv);
            await _db.SaveChangesAsync();
            TempData["ThanhCong"] = "Đã xóa bài viết";
        }
        return RedirectToAction(nameof(BaiViet));
    }

    [HttpPost, Route("quan-tri/bai-viet/{id:int}/doi-trang-thai")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiTrangThaiBaiViet(int id)
    {
        if (!QuyenHelper.CoQuyen(User, "baiviet")) return Forbid();
        var bv = await _db.BaiViets.FindAsync(id);
        if (bv != null)
        {
            bv.DaDang = !bv.DaDang;
            await _db.SaveChangesAsync();
            TempData["ThanhCong"] = bv.DaDang ? "Đã đăng bài viết" : "Đã ẩn bài viết";
        }
        return RedirectToAction(nameof(BaiViet));
    }

    [Route("quan-tri/lien-he")]
    public async Task<IActionResult> LienHe(string? trangThai)
    {
        if (!QuyenHelper.CoQuyen(User, "lienhe")) return Forbid();
        var query = _db.LienHes.AsQueryable();
        if (!string.IsNullOrWhiteSpace(trangThai)) query = query.Where(x => x.TrangThai == trangThai);
        ViewBag.TrangThai = trangThai;
        ViewBag.SoMoi = await _db.LienHes.CountAsync(x => x.TrangThai == "Moi");
        return View(await query.OrderByDescending(x => x.NgayTao).ToListAsync());
    }

    [HttpPost, Route("quan-tri/lien-he/{id:int}/cap-nhat")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CapNhatLienHe(int id, string trangThai, string ghiChuXuLy)
    {
        if (!QuyenHelper.CoQuyen(User, "lienhe")) return Forbid();
        var lh = await _db.LienHes.FindAsync(id);
        if (lh == null) return NotFound();
        var hopLe = new[] { "Moi", "DangXuLy", "DaXuLy" };
        if (!hopLe.Contains(trangThai)) return BadRequest();
        lh.TrangThai = trangThai;
        lh.GhiChuXuLy = ghiChuXuLy?.Trim() ?? "";
        await _db.SaveChangesAsync();
        TempData["ThanhCong"] = "Đã cập nhật liên hệ";
        return RedirectToAction(nameof(LienHe));
    }
}
