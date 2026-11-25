using GIADUNGCHATLUONG.DuLieu;
using GIADUNGCHATLUONG.TienIch;
using GIADUNGCHATLUONG.MoHinh;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GIADUNGCHATLUONG.DieuKhien;

/// <summary>Phần chính của khu vực quản trị: dashboard và các chức năng quản lý tổng quát.</summary>
[Authorize(Roles = "Admin,AdminPhu,NhanVien")]
public partial class QuanTriController : Controller
{
    private readonly AppDb _db;
    private readonly IWebHostEnvironment _env;
    public QuanTriController(AppDb coSoDuLieu, IWebHostEnvironment moiTruongWebsite)
    {
        _db = coSoDuLieu; _env = moiTruongWebsite;
    }


    [Route("quan-tri")]
    public async Task<IActionResult> Dashboard()
    {
        if (!QuyenHelper.CoQuyen(User, "baocao"))
        {
            if (QuyenHelper.CoQuyen(User, "sanpham")) return RedirectToAction(nameof(SanPham));
            if (QuyenHelper.CoQuyen(User, "donhang")) return RedirectToAction(nameof(DonHang));
            if (QuyenHelper.CoQuyen(User, "danhgia")) return RedirectToAction(nameof(DanhGia));
            if (QuyenHelper.CoQuyen(User, "kho")) return RedirectToAction(nameof(KhoHang));
            if (QuyenHelper.CoQuyen(User, "danhmuc")) return RedirectToAction(nameof(DanhMuc));
            if (QuyenHelper.CoQuyen(User, "lienhe")) return RedirectToAction(nameof(LienHe));
            if (QuyenHelper.CoQuyen(User, "banner")) return RedirectToAction(nameof(Banner));
            if (QuyenHelper.CoQuyen(User, "baiviet")) return RedirectToAction(nameof(BaiViet));
            return Forbid();
        }
        var homNay = DateTime.Today;
        var donHangs = await _db.DonHangs.Include(x => x.ChiTiet).Include(x => x.NguoiDung).ToListAsync();
        var sps = await _db.SanPhams.ToListAsync();

        var daThanhToan = donHangs.Where(x => x.TrangThai != TrangThaiDon.DaHuy && (
            x.ThanhToan == ThanhToan.DaThanhToan ||
            (x.TrangThai == TrangThaiDon.HoanThanh && x.PhuongThucThanhToan == PhuongThucThanhToan.COD))).ToList();

        ViewBag.DoanhThuHomNay = daThanhToan.Where(x => x.NgayTao.Date == homNay).Sum(x => x.TongTien);
        ViewBag.DoanhThuThang  = daThanhToan.Where(x => x.NgayTao.Month == homNay.Month && x.NgayTao.Year == homNay.Year).Sum(x => x.TongTien);
        ViewBag.SoDonChoXacNhan = donHangs.Count(x => x.TrangThai == TrangThaiDon.ChoXacNhan);
        ViewBag.SoDonDangGiao   = donHangs.Count(x => x.TrangThai == TrangThaiDon.DangGiao);
        ViewBag.SoDonHoanThanh  = donHangs.Count(x => x.TrangThai == TrangThaiDon.HoanThanh);
        ViewBag.SoDonDaHuy      = donHangs.Count(x => x.TrangThai == TrangThaiDon.DaHuy);
        ViewBag.TongDon         = donHangs.Count;
        ViewBag.TongSanPham     = sps.Count;
        ViewBag.TongKhachHang   = await _db.NguoiDungs.CountAsync(x => x.VaiTro == VaiTro.KhachHang);
        ViewBag.TongTonKho      = sps.Sum(x => x.TonKho);
        ViewBag.GiaTriTonKho    = sps.Sum(x => x.GiaHienTai * x.TonKho);
        ViewBag.SanPhamSapHet   = sps.Where(x => x.TonKho > 0 && x.TonKho <= 5).OrderBy(x => x.TonKho).Take(5).ToList();
        ViewBag.SanPhamHetHang  = sps.Where(x => x.TonKho <= 0).OrderBy(x => x.Ten).Take(5).ToList();
        ViewBag.DonMoiNhat      = donHangs.OrderByDescending(x => x.NgayTao).Take(6).ToList();
        ViewBag.TopKhachHang = daThanhToan.Where(x => x.NguoiDungId.HasValue)
            .GroupBy(x => x.NguoiDungId)
            .Select(g => new
            {
                Ten = g.OrderByDescending(x => x.NgayTao).Select(x => x.NguoiDung?.HoTen ?? x.HoTen).FirstOrDefault() ?? "Khách hàng",
                SoDon = g.Count(), TongChi = g.Sum(x => x.TongTien)
            })
            .OrderByDescending(x => x.TongChi).Take(5).ToList();

        return View();
    }

    [Route("quan-tri/san-pham")]
    public async Task<IActionResult> SanPham(string? q, int trang = 1)
    {
        if (!QuyenHelper.CoQuyen(User, "sanpham")) return Forbid();
        var query = _db.SanPhams.Include(x => x.DanhMuc).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.Ten.Contains(q.Trim()));
        ViewBag.Q = q;

        const int soSanPhamMoiTrang = 10;
        var tongSanPham = await query.CountAsync();
        var tongSoTrang = Math.Max(1, (int)Math.Ceiling(tongSanPham / (double)soSanPhamMoiTrang));
        trang = Math.Clamp(trang, 1, tongSoTrang);
        ViewBag.TongSanPham = tongSanPham;
        ViewBag.TrangHienTai = trang;
        ViewBag.TongSoTrang = tongSoTrang;

        var sanPhamTrongTrang = await query.OrderByDescending(x => x.Id)
            .Skip((trang - 1) * soSanPhamMoiTrang)
            .Take(soSanPhamMoiTrang)
            .ToListAsync();
        return View(sanPhamTrongTrang);
    }

    [HttpGet, Route("quan-tri/san-pham/them")]
    [HttpGet, Route("quan-tri/san-pham/{id:int}/sua")]
    public async Task<IActionResult> SuaSanPham(int? id)
    {
        if (!QuyenHelper.CoQuyen(User, "sanpham")) return Forbid();
        ViewBag.DanhMucs = await _db.DanhMucs.ToListAsync();
        if (id == null) return View(new SanPham { DangBan = true });
        var sp = await _db.SanPhams.FindAsync(id);
        if (sp == null) return NotFound();
        return View(sp);
    }

    [HttpPost, Route("quan-tri/san-pham/luu")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LuuSanPham(SanPham input, IFormFile? anhFile)
    {
        if (!QuyenHelper.CoQuyen(User, "sanpham")) return Forbid();

        if (string.IsNullOrWhiteSpace(input.Ten) || input.GiaGoc <= 0 || input.TonKho < 0
            || !await _db.DanhMucs.AnyAsync(x => x.Id == input.DanhMucId))
        {
            TempData["Loi"] = "Vui lòng nhập tên, chọn danh mục hợp lệ, giá > 0 và tồn kho >= 0";
            ViewBag.DanhMucs = await _db.DanhMucs.ToListAsync();
            return View("SuaSanPham", input);
        }

        SanPham sp;
        if (input.Id == 0) { sp = new SanPham(); _db.SanPhams.Add(sp); }
        else
        {
            sp = await _db.SanPhams.FindAsync(input.Id);
            if (sp == null) return NotFound();
        }

        if (anhFile != null && anhFile.Length > 0)
        {
            var duoiTep = Path.GetExtension(anhFile.FileName).ToLowerInvariant();
            var choPhep = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            if (!choPhep.Contains(duoiTep))
            {
                TempData["Loi"] = "Chỉ chấp nhận ảnh JPG/PNG/WEBP/GIF";
                ViewBag.DanhMucs = await _db.DanhMucs.ToListAsync();
                return View("SuaSanPham", input);
            }
            if (anhFile.Length > 5 * 1024 * 1024)
            {
                TempData["Loi"] = "Ảnh tối đa 5MB";
                ViewBag.DanhMucs = await _db.DanhMucs.ToListAsync();
                return View("SuaSanPham", input);
            }
            var duLieuDauTep = new byte[12];
            using (var luongAnh = anhFile.OpenReadStream())
                _ = await luongAnh.ReadAsync(duLieuDauTep.AsMemory(0, duLieuDauTep.Length));
            var anhHopLe = duoiTep switch
            {
                ".jpg" or ".jpeg" => duLieuDauTep[0] == 0xFF && duLieuDauTep[1] == 0xD8 && duLieuDauTep[2] == 0xFF,
                ".png" => duLieuDauTep[0] == 0x89 && duLieuDauTep[1] == 0x50 && duLieuDauTep[2] == 0x4E && duLieuDauTep[3] == 0x47,
                ".gif" => System.Text.Encoding.ASCII.GetString(duLieuDauTep, 0, 3) == "GIF",
                ".webp" => System.Text.Encoding.ASCII.GetString(duLieuDauTep, 0, 4) == "RIFF"
                    && System.Text.Encoding.ASCII.GetString(duLieuDauTep, 8, 4) == "WEBP",
                _ => false
            };
            if (!anhHopLe)
            {
                TempData["Loi"] = "Nội dung tệp không phải ảnh hợp lệ hoặc phần mở rộng không khớp.";
                ViewBag.DanhMucs = await _db.DanhMucs.ToListAsync();
                return View("SuaSanPham", input);
            }
            var thuMuc = Path.Combine(_env.WebRootPath, "TaiLen", "san-pham");
            Directory.CreateDirectory(thuMuc);
            var tenMoi = Guid.NewGuid().ToString("N") + duoiTep;
            using (var tepDauRa = System.IO.File.Create(Path.Combine(thuMuc, tenMoi)))
                await anhFile.CopyToAsync(tepDauRa);
            sp.AnhChinh = "/TaiLen/san-pham/" + tenMoi;
        }
        else if (!string.IsNullOrWhiteSpace(input.AnhChinh))
        {
            var anh = input.AnhChinh.Trim();
            if (!((anh.StartsWith("/", StringComparison.Ordinal) && !anh.StartsWith("//", StringComparison.Ordinal)) ||
                  (Uri.TryCreate(anh, UriKind.Absolute, out var uriAnh) &&
                   (uriAnh.Scheme == Uri.UriSchemeHttp || uriAnh.Scheme == Uri.UriSchemeHttps))))
            {
                TempData["Loi"] = "Đường dẫn ảnh phải là đường dẫn nội bộ hoặc URL HTTP/HTTPS hợp lệ.";
                ViewBag.DanhMucs = await _db.DanhMucs.ToListAsync();
                return View("SuaSanPham", input);
            }
            sp.AnhChinh = anh;
        }

        sp.Ten = input.Ten.Trim();
        sp.MoTa = input.MoTa?.Trim() ?? "";
        sp.CongDungChinh = input.CongDungChinh?.Trim() ?? "";
        sp.DoiTuongSuDung = input.DoiTuongSuDung?.Trim() ?? "";
        sp.TinhNangNoiBat = input.TinhNangNoiBat?.Trim() ?? "";
        sp.LuuYSuDung = input.LuuYSuDung?.Trim() ?? "";
        sp.ThongSo = input.ThongSo?.Trim() ?? "";
        sp.GiaGoc = input.GiaGoc;
        sp.GiaKhuyenMai = input.GiaKhuyenMai > 0 && input.GiaKhuyenMai < input.GiaGoc ? input.GiaKhuyenMai : null;
        sp.TonKho = input.TonKho;
        sp.DanhMucId = input.DanhMucId;
        sp.DangBan = input.DangBan;
        sp.NoiBat = input.NoiBat;
        await _db.SaveChangesAsync();
        TempData["ThanhCong"] = input.Id == 0 ? "Đã thêm sản phẩm" : "Đã cập nhật sản phẩm";
        return RedirectToAction(nameof(SanPham));
    }

    [HttpPost, Route("quan-tri/san-pham/{id:int}/xoa")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaSanPham(int id)
    {
        if (!QuyenHelper.CoQuyen(User, "sanpham")) return Forbid();
        var sp = await _db.SanPhams.FindAsync(id);
        if (sp != null)
        {
            sp.DangBan = false;
            await _db.SaveChangesAsync();
            TempData["ThanhCong"] = "Đã ẩn sản phẩm";
        }
        return RedirectToAction(nameof(SanPham));
    }

    [HttpPost, Route("quan-tri/san-pham/{id:int}/hien")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HienSanPham(int id)
    {
        if (!QuyenHelper.CoQuyen(User, "sanpham")) return Forbid();
        var sp = await _db.SanPhams.FindAsync(id);
        if (sp != null)
        {
            sp.DangBan = !sp.DangBan;
            await _db.SaveChangesAsync();
            TempData["ThanhCong"] = "Đã đổi trạng thái";
        }
        return RedirectToAction(nameof(SanPham));
    }
}
