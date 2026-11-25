using GIADUNGCHATLUONG.DuLieu;
using GIADUNGCHATLUONG.TienIch;
using GIADUNGCHATLUONG.MoHinh;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GIADUNGCHATLUONG.DieuKhien;

/// <summary>Xử lý dữ liệu cần hiển thị ở trang chủ và các trang thông tin chung.</summary>
public class TrangChuController : Controller
{
    private readonly AppDb _db;
    public TrangChuController(AppDb coSoDuLieu) => _db = coSoDuLieu;

    public async Task<IActionResult> Index()
    {
        ViewBag.Banners = await _db.Banners
            .Where(x => x.DangHienThi)
            .OrderBy(x => x.ThuTu).ThenBy(x => x.Id)
            .ToListAsync();

        ViewBag.DanhMucs = await _db.DanhMucs.ToListAsync();
        ViewBag.CauHinh = await _db.CauHinhs.FirstOrDefaultAsync();

        var noiBat = await _db.SanPhams
            .Include(x => x.DanhMuc).Include(x => x.DanhSachDanhGia)
            .Where(x => x.DangBan && x.NoiBat && x.TonKho > 0)
            .OrderByDescending(x => x.NgayTao).Take(8).ToListAsync();
        ViewBag.SanPhamNoiBat = noiBat;

        var sanPhamMoi = await _db.SanPhams
            .Include(x => x.DanhMuc).Include(x => x.DanhSachDanhGia)
            .Where(x => x.DangBan && x.TonKho > 0)
            .OrderByDescending(x => x.NgayTao).Take(8).ToListAsync();
        ViewBag.SanPhamMoi = sanPhamMoi;

        var banChay = await _db.SanPhams
            .Include(x => x.DanhMuc).Include(x => x.DanhSachDanhGia)
            .Where(x => x.DangBan && x.TonKho > 0)
            .OrderByDescending(x => x.DaBan).Take(4).ToListAsync();
        ViewBag.SanPhamBanChay = banChay;

        // Chuẩn bị ảnh cho vòng xoay sản phẩm 3D.
        var allImgs = await _db.SanPhams.Where(x => x.DangBan && !string.IsNullOrEmpty(x.AnhChinh))
            .Select(x => x.AnhChinh).ToListAsync();
        if (!allImgs.Any()) allImgs.Add("/HinhAnh/anh-mac-dinh.svg");
        var list37 = new List<string>();
        for (int i = 0; i < 37; i++) list37.Add(allImgs[i % allImgs.Count]);
        ViewBag.List37 = list37;

        return View();
    }

    [Route("bang-xep-hang")]
    public async Task<IActionResult> BangXepHang()
    {
        var cauHinh = await _db.CauHinhs.FirstOrDefaultAsync() ?? new CauHinh();
        var giaoDich = await _db.DonHangs
            .Where(x => x.NguoiDungId != null && x.TrangThai != TrangThaiDon.DaHuy
                && (x.ThanhToan == ThanhToan.DaThanhToan
                    || (x.TrangThai == TrangThaiDon.HoanThanh && x.PhuongThucThanhToan == PhuongThucThanhToan.COD)))
            .ToListAsync();
        // Gom theo mã tài khoản, không theo tên được lưu trên đơn để tránh tách hạng.
        // Cách này vẫn giữ đúng hạng nếu khách hàng đổi họ tên giữa các lần mua.
        var hang = giaoDich.GroupBy(x => x.NguoiDungId!.Value)
            .Select(g => new KhachHangXepHangVM
            {
                NguoiDungId = g.Key,
                HoTen = g.OrderByDescending(x => x.NgayTao).Select(x => x.HoTen).FirstOrDefault() ?? "Khách hàng",
                SoDon = g.Count(),
                TongChi = g.Sum(x => x.TongTien),
                Diem = (int)(g.Sum(x => x.TongTien) / 10000m)
            })
            .OrderByDescending(x => x.TongChi).ThenByDescending(x => x.SoDon).Take(20).ToList();
        var quatang = new[] { cauHinh.QuaHangNhat, cauHinh.QuaHangNhi, cauHinh.QuaHangBa };
        for (var i = 0; i < hang.Count; i++) hang[i].Qua = i < 3 ? quatang[i] : "Huy hiệu khách hàng thân thiết";
        ViewBag.CauHinh = cauHinh;
        ViewBag.TongGiaoDich = giaoDich.Count;
        ViewBag.TongDoanhThu = giaoDich.Sum(x => x.TongTien);
        return View(hang);
    }

    [Route("lien-he")]
    public IActionResult LienHe() => View();

    [HttpPost, Route("lien-he")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LienHe(string hoTen, string email, string dienThoai, string noiDung)
    {
        if (string.IsNullOrWhiteSpace(hoTen) || string.IsNullOrWhiteSpace(noiDung))
        {
            TempData["Loi"] = "Vui lòng nhập họ tên và nội dung";
            return View();
        }
        _db.LienHes.Add(new LienHe
        {
            HoTen = hoTen.Trim(),
            Email = email?.Trim() ?? "",
            DienThoai = dienThoai?.Trim() ?? "",
            NoiDung = noiDung.Trim()
        });
        await _db.SaveChangesAsync();
        TempData["ThanhCong"] = "Cảm ơn bạn đã gửi liên hệ! Chúng tôi sẽ phản hồi sớm.";
        return RedirectToAction(nameof(LienHe));
    }

    [Route("loi")]
    public IActionResult Loi() => Content("Đã có lỗi xảy ra.");
}
