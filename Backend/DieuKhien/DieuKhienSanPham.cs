using GIADUNGCHATLUONG.DuLieu;
using GIADUNGCHATLUONG.TienIch;
using GIADUNGCHATLUONG.MoHinh;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TienIchHelper = GIADUNGCHATLUONG.TienIch.TienIch;

namespace GIADUNGCHATLUONG.DieuKhien;

/// <summary>Xử lý danh sách, tìm kiếm, lọc và xem chi tiết sản phẩm.</summary>
public class SanPhamController : Controller
{
    private readonly AppDb _db;
    public SanPhamController(AppDb coSoDuLieu) => _db = coSoDuLieu;

    [Route("san-pham")]
    public async Task<IActionResult> Index(string? q, int? dm, string? sapXep, decimal? min, decimal? max, int trang = 1)
    {
        var query = _db.SanPhams
            .Include(x => x.DanhMuc).Include(x => x.DanhSachDanhGia)
            .Where(x => x.DangBan);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.Ten.Contains(q.Trim())
                || x.MoTa.Contains(q.Trim())
                || x.CongDungChinh.Contains(q.Trim())
                || x.TinhNangNoiBat.Contains(q.Trim()));
        if (dm.HasValue) query = query.Where(x => x.DanhMucId == dm.Value);
        if (min.HasValue) query = query.Where(x =>
            (x.GiaKhuyenMai.HasValue && x.GiaKhuyenMai.Value < x.GiaGoc ? x.GiaKhuyenMai.Value : x.GiaGoc) >= min.Value);
        if (max.HasValue) query = query.Where(x =>
            (x.GiaKhuyenMai.HasValue && x.GiaKhuyenMai.Value < x.GiaGoc ? x.GiaKhuyenMai.Value : x.GiaGoc) <= max.Value);

        query = sapXep switch
        {
            "gia-tang" => query.OrderBy(x => x.GiaKhuyenMai.HasValue && x.GiaKhuyenMai.Value < x.GiaGoc ? x.GiaKhuyenMai.Value : x.GiaGoc),
            "gia-giam" => query.OrderByDescending(x => x.GiaKhuyenMai.HasValue && x.GiaKhuyenMai.Value < x.GiaGoc ? x.GiaKhuyenMai.Value : x.GiaGoc),
            "ban-chay" => query.OrderByDescending(x => x.DaBan),
            _          => query.OrderByDescending(x => x.NgayTao)
        };

        ViewBag.DanhMucs = await _db.DanhMucs.ToListAsync();
        ViewBag.Q = q;
        ViewBag.Dm = dm;
        ViewBag.SapXep = sapXep;
        ViewBag.Min = min;
        ViewBag.Max = max;
        ViewBag.CauHinh = await _db.CauHinhs.FirstOrDefaultAsync();

        // Phân trang ở cơ sở dữ liệu để không tải toàn bộ sản phẩm lên bộ nhớ.
        const int soSanPhamMoiTrang = 12;
        var tongSanPham = await query.CountAsync();
        var tongSoTrang = Math.Max(1, (int)Math.Ceiling(tongSanPham / (double)soSanPhamMoiTrang));
        trang = Math.Clamp(trang, 1, tongSoTrang);
        ViewBag.TongSanPham = tongSanPham;
        ViewBag.TrangHienTai = trang;
        ViewBag.TongSoTrang = tongSoTrang;

        var sanPhamTrongTrang = await query
            .Skip((trang - 1) * soSanPhamMoiTrang)
            .Take(soSanPhamMoiTrang)
            .ToListAsync();

        return View(sanPhamTrongTrang);
    }

    [Route("san-pham/{id:int}")]
    public async Task<IActionResult> ChiTiet(int id)
    {
        // Chỉ đánh dấu/hiển thị đánh giá khi tài khoản đã nhận hàng và thanh toán cho đúng sản phẩm.
        var nguoiDaMua = await _db.ChiTietDonHangs
            .Join(_db.DonHangs.Where(x => x.TrangThai == TrangThaiDon.HoanThanh
                    && x.ThanhToan == ThanhToan.DaThanhToan && x.NguoiDungId.HasValue),
                ct => ct.DonHangId, donHang => donHang.Id,
                (ct, donHang) => new { donHang.NguoiDungId, ct.SanPhamId })
            .Where(x => x.SanPhamId == id)
            .Select(x => x.NguoiDungId!.Value).Distinct().ToListAsync();

        var truyVan = _db.SanPhams
            .Include(x => x.DanhMuc)
            .Include(x => x.DanhSachDanhGia.Where(d => d.DaDuyet && nguoiDaMua.Contains(d.NguoiDungId)))
                .ThenInclude(x => x.NguoiDung)
            .AsQueryable();
        if (!QuyenHelper.CoQuyen(User, "sanpham"))
            truyVan = truyVan.Where(x => x.DangBan);
        var sp = await truyVan.FirstOrDefaultAsync(x => x.Id == id);

        if (sp == null) return NotFound();
        foreach (var review in sp.DanhSachDanhGia) review.DaMuaXacMinh = true;

        var uid = TienIchHelper.LayIdNguoiDung(User);
        ViewBag.YeuThich = uid.HasValue &&
            await _db.YeuThichs.AnyAsync(x => x.NguoiDungId == uid.Value && x.SanPhamId == id);
        ViewBag.DaMua = uid.HasValue && nguoiDaMua.Contains(uid.Value);
        ViewBag.DaDanhGia = uid.HasValue &&
            await _db.DanhGias.AnyAsync(x => x.NguoiDungId == uid.Value && x.SanPhamId == id);
        ViewBag.SanPhamLienQuan = await _db.SanPhams
            .Include(x => x.DanhSachDanhGia)
            .Where(x => x.DangBan && x.DanhMucId == sp.DanhMucId && x.Id != sp.Id)
            .Take(4).ToListAsync();
        ViewBag.CauHinh = await _db.CauHinhs.FirstOrDefaultAsync();

        return View(sp);
    }

    [HttpPost, Authorize, Route("danh-gia")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DanhGia(int sanPhamId, int soSao, string tieuDe, string noiDung)
    {
        var uid = TienIchHelper.LayIdNguoiDung(User);
        if (uid == null) return RedirectToAction("DangNhap", "TaiKhoan");

        var daMua = await _db.DonHangs
            .Where(x => x.NguoiDungId == uid.Value && x.TrangThai == TrangThaiDon.HoanThanh
                && x.ThanhToan == ThanhToan.DaThanhToan)
            .SelectMany(x => x.ChiTiet).AnyAsync(x => x.SanPhamId == sanPhamId);
        if (!daMua)
        {
            TempData["Loi"] = "Bạn cần mua và nhận sản phẩm để đánh giá";
            return RedirectToAction(nameof(ChiTiet), new { id = sanPhamId });
        }
        if (await _db.DanhGias.AnyAsync(x => x.NguoiDungId == uid && x.SanPhamId == sanPhamId))
        {
            TempData["Loi"] = "Bạn đã đánh giá sản phẩm này rồi";
            return RedirectToAction(nameof(ChiTiet), new { id = sanPhamId });
        }
        if (string.IsNullOrWhiteSpace(tieuDe) || string.IsNullOrWhiteSpace(noiDung)
            || tieuDe.Trim().Length > 120 || noiDung.Trim().Length > 1000)
        {
            TempData["Loi"] = "Vui lòng nhập tiêu đề và nội dung (tối đa 120/1.000 ký tự)";
            return RedirectToAction(nameof(ChiTiet), new { id = sanPhamId });
        }

        _db.DanhGias.Add(new DanhGia
        {
            SanPhamId = sanPhamId, NguoiDungId = uid.Value,
            SoSao = Math.Clamp(soSao, 1, 5),
            TieuDe = tieuDe.Trim(), NoiDung = noiDung.Trim(), DaDuyet = false
        });
        await _db.SaveChangesAsync();
        TempData["ThanhCong"] = "Đánh giá đã được gửi và sẽ hiển thị sau khi cửa hàng duyệt.";
        return RedirectToAction(nameof(ChiTiet), new { id = sanPhamId });
    }

    [HttpPost, Authorize, Route("yeu-thich")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> YeuThich(int sanPhamId, string? returnUrl)
    {
        var uid = TienIchHelper.LayIdNguoiDung(User);
        if (uid == null) return RedirectToAction("DangNhap", "TaiKhoan");

        var yt = await _db.YeuThichs.FirstOrDefaultAsync(x => x.NguoiDungId == uid && x.SanPhamId == sanPhamId);
        if (yt != null) _db.YeuThichs.Remove(yt);
        else _db.YeuThichs.Add(new YeuThich { NguoiDungId = uid.Value, SanPhamId = sanPhamId });
        await _db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction(nameof(ChiTiet), new { id = sanPhamId });
    }

    [Authorize, Route("yeu-thich-cua-toi")]
    public async Task<IActionResult> YeuThichCuaToi()
    {
        var uid = TienIchHelper.LayIdNguoiDung(User);
        if (uid == null) return RedirectToAction("DangNhap", "TaiKhoan");
        var list = await _db.YeuThichs
            .Include(x => x.SanPham).ThenInclude(s => s.DanhSachDanhGia)
            .Where(x => x.NguoiDungId == uid && x.SanPham.DangBan)
            .Select(x => x.SanPham).ToListAsync();
        return View(list);
    }
}
