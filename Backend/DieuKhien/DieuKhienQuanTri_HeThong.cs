using System.Text;
using GIADUNGCHATLUONG.TienIch;
using GIADUNGCHATLUONG.MoHinh;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GIADUNGCHATLUONG.DieuKhien;

public partial class QuanTriController
{
    [Route("quan-tri/thong-ke")]
    public async Task<IActionResult> ThongKe(DateTime? tuNgay, DateTime? denNgay)
    {
        if (!QuyenHelper.CoQuyen(User, "baocao")) return Forbid();
        var den = (denNgay ?? DateTime.Today).Date;
        var tu  = (tuNgay ?? den.AddDays(-29)).Date;
        if (tu > den) (tu, den) = (den, tu);
        ViewBag.TuNgay = tu; ViewBag.DenNgay = den;

        var donHangs = await _db.DonHangs.Include(x => x.ChiTiet).ToListAsync();
        var trongKhoang = donHangs.Where(x => x.NgayTao.Date >= tu && x.NgayTao.Date <= den).ToList();
        var daThanhToan = trongKhoang.Where(x => x.TrangThai != TrangThaiDon.DaHuy && (
            x.ThanhToan == ThanhToan.DaThanhToan ||
            (x.TrangThai == TrangThaiDon.HoanThanh && x.PhuongThucThanhToan == PhuongThucThanhToan.COD))).ToList();

        ViewBag.DoanhThu = daThanhToan.Sum(x => x.TongTien);
        ViewBag.SoDon = daThanhToan.Count;
        ViewBag.DonHuy = trongKhoang.Count(x => x.TrangThai == TrangThaiDon.DaHuy);

        var bieuDo = new List<(string Ngay, decimal Doanh)>();
        for (var d = den.AddDays(-6); d <= den; d = d.AddDays(1))
        {
            var cuaNgay = daThanhToan.Where(x => x.NgayTao.Date == d).Sum(x => x.TongTien);
            bieuDo.Add((d.ToString("dd/MM"), cuaNgay));
        }
        ViewBag.BieuDo = bieuDo;
        ViewBag.BieuDoMax = Math.Max(1, bieuDo.Any() ? bieuDo.Max(x => x.Doanh) : 0);

        ViewBag.TopSanPham = daThanhToan.SelectMany(x => x.ChiTiet)
            .GroupBy(x => x.SanPhamId)
            .Select(g => new { Ten = g.First().TenSanPham, SoLuong = g.Sum(x => x.SoLuong), DoanhThu = g.Sum(x => x.ThanhTien) })
            .OrderByDescending(x => x.SoLuong).Take(10).ToList();
        ViewBag.TopKhachHang = daThanhToan.Where(x => x.NguoiDungId.HasValue)
            .GroupBy(x => x.NguoiDungId)
            .Select(g => new { Ten = g.OrderByDescending(x => x.NgayTao).First().HoTen, SoDon = g.Count(), TongChi = g.Sum(x => x.TongTien) })
            .OrderByDescending(x => x.TongChi).Take(10).ToList();

        var sps = await _db.SanPhams.ToListAsync();
        ViewBag.ConHang = sps.Count(x => x.TonKho > 5);
        ViewBag.SapHet = sps.Count(x => x.TonKho > 0 && x.TonKho <= 5);
        ViewBag.HetHang = sps.Count(x => x.TonKho <= 0);
        ViewBag.SanPhamSapHet = sps.Where(x => x.TonKho <= 5).OrderBy(x => x.TonKho).Take(10).ToList();

        return View();
    }

    [Route("quan-tri/thong-ke/xuat-csv")]
    public async Task<IActionResult> XuatCsv(DateTime? tuNgay, DateTime? denNgay)
    {
        if (!QuyenHelper.CoQuyen(User, "baocao")) return Forbid();
        var den = (denNgay ?? DateTime.Today).Date;
        var tu  = (tuNgay ?? den.AddDays(-29)).Date;

        var donHangs = await _db.DonHangs
            .Where(x => x.NgayTao.Date >= tu && x.NgayTao.Date <= den
                && x.TrangThai != TrangThaiDon.DaHuy
                && (x.ThanhToan == ThanhToan.DaThanhToan
                    || (x.TrangThai == TrangThaiDon.HoanThanh && x.PhuongThucThanhToan == PhuongThucThanhToan.COD)))
            .ToListAsync();

        var sb = new StringBuilder("Ngay,SoDon,DoanhThu\n");
        for (var d = tu; d <= den; d = d.AddDays(1))
        {
            var cuaNgay = donHangs.Where(x => x.NgayTao.Date == d).ToList();
            sb.Append($"{d:dd/MM/yyyy},{cuaNgay.Count},{(long)cuaNgay.Sum(x => x.TongTien)}\n");
        }
        sb.Append($"TONG,{donHangs.Count},{(long)donHangs.Sum(x => x.TongTien)}\n");
        return File(new UTF8Encoding(true).GetBytes(sb.ToString()), "text/csv",
            $"doanh-thu_{tu:yyyyMMdd}_{den:yyyyMMdd}.csv");
    }

    [Route("quan-tri/kho-hang")]
    public async Task<IActionResult> KhoHang(string? q, string? tinhTrang)
    {
        if (!QuyenHelper.CoQuyen(User, "kho")) return Forbid();
        var query = _db.SanPhams.Include(x => x.DanhMuc).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.Ten.Contains(q.Trim()));
        query = tinhTrang switch
        {
            "sap-het" => query.Where(x => x.TonKho > 0 && x.TonKho <= 5),
            "het" => query.Where(x => x.TonKho <= 0),
            "con" => query.Where(x => x.TonKho > 5),
            _ => query
        };
        var all = await _db.SanPhams.ToListAsync();
        var donDangXuLy = await _db.DonHangs
            .Where(x => x.TrangThai == TrangThaiDon.ChoXacNhan
                || x.TrangThai == TrangThaiDon.DaXacNhan
                || x.TrangThai == TrangThaiDon.DangGiao)
            .SelectMany(x => x.ChiTiet.Select(ct => new { ct.SanPhamId, ct.SoLuong }))
            .GroupBy(x => x.SanPhamId)
            .Select(g => new { SanPhamId = g.Key, SoLuong = g.Sum(x => x.SoLuong) })
            .ToDictionaryAsync(x => x.SanPhamId, x => x.SoLuong);
        ViewBag.DonDangXuLy = donDangXuLy;
        ViewBag.TongDonDangXuLy = donDangXuLy.Values.Sum();
        ViewBag.TongSanPham = all.Count;
        ViewBag.TongDonVi = all.Sum(x => x.TonKho);
        ViewBag.GiaTriTon = all.Sum(x => x.GiaHienTai * x.TonKho);
        ViewBag.SapHet = all.Count(x => x.TonKho is > 0 and <= 5);
        ViewBag.Het = all.Count(x => x.TonKho <= 0);
        ViewBag.Q = q; ViewBag.TinhTrang = tinhTrang;
        return View(await query.OrderBy(x => x.TonKho).ThenBy(x => x.Ten).ToListAsync());
    }

    [Authorize(Roles = "Admin"), Route("quan-tri/cau-hinh")]
    public async Task<IActionResult> CauHinh()
    {
        var ch = await _db.CauHinhs.FirstOrDefaultAsync() ?? new CauHinh();
        return View(ch);
    }

    [HttpPost, Authorize(Roles = "Admin"), Route("quan-tri/cau-hinh")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CauHinh(CauHinh input)
    {
        var ch = await _db.CauHinhs.FirstOrDefaultAsync();
        if (ch == null) { ch = new CauHinh(); _db.CauHinhs.Add(ch); }
        ch.TenShop     = input.TenShop?.Trim() ?? "GIADUNGCHATLUONG";
        ch.Slogan      = input.Slogan?.Trim() ?? "";
        ch.DiaChi      = input.DiaChi?.Trim() ?? "";
        ch.DienThoai   = input.DienThoai?.Trim() ?? "";
        ch.Email       = input.Email?.Trim() ?? "";
        ch.GioMoCua    = input.GioMoCua?.Trim() ?? "";
        ch.NganHang    = input.NganHang?.Trim() ?? "";
        ch.SoTaiKhoan  = input.SoTaiKhoan?.Trim() ?? "";
        ch.ChuTaiKhoan = input.ChuTaiKhoan?.Trim() ?? "";
        ch.MaQr        = input.MaQr?.Trim() ?? "";
        ch.MauChuDao = MauHex(input.MauChuDao, "#2563EB");
        ch.MauPhu = MauHex(input.MauPhu, "#0F766E");
        ch.MauNen = MauHex(input.MauNen, "#F7F8FC");
        ch.MauChu = MauHex(input.MauChu, "#182235");
        ch.MauThe = MauHex(input.MauThe, "#FFFFFF");
        ch.KieuBoCuc = input.KieuBoCuc == "gon" ? "gon" : "rong";
        ch.KieuChu = input.KieuChu is "arial" or "serif" or "rounded" ? input.KieuChu : "system";
        ch.CoChu = input.CoChu is "nho" or "lon" ? input.CoChu : "binhthuong";
        ch.DoRongTrang = Math.Clamp(input.DoRongTrang, 1100, 1600);
        ch.SoCotSanPham = Math.Clamp(input.SoCotSanPham, 2, 5);
        ch.KieuThe = input.KieuThe == "vuong" ? "vuong" : input.KieuThe == "tron" ? "tron" : "mem";
        ch.KieuBong = input.KieuBong is "vua" or "tat" ? input.KieuBong : "nhe";
        ch.CauTrucTrangChu = ChuanHoaCauTruc(input.CauTrucTrangChu);
        ch.CacKhoiAn = ChuanHoaKhoiAn(input.CacKhoiAn);
        ch.QuaHangNhat = Cat(input.QuaHangNhat, "Voucher mua sắm 500.000đ");
        ch.QuaHangNhi = Cat(input.QuaHangNhi, "Voucher mua sắm 300.000đ");
        ch.QuaHangBa = Cat(input.QuaHangBa, "Voucher mua sắm 150.000đ");
        await _db.SaveChangesAsync();
        TempData["ThanhCong"] = "Đã lưu cấu hình";
        return RedirectToAction(nameof(CauHinh));
    }

    [Authorize(Roles = "Admin"), Route("quan-tri/nhat-ky")]
    public async Task<IActionResult> NhatKy(string? q, string? loai)
    {
        var query = _db.NhatKys.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.NguoiDung.Contains(q) || x.HanhDong.Contains(q) || x.ChiTiet.Contains(q)
                || x.VaiTro.Contains(q) || x.KetQua.Contains(q) || x.DuongDan.Contains(q));
        if (!string.IsNullOrWhiteSpace(loai)) query = query.Where(x => x.Loai == loai);
        ViewBag.Q = q; ViewBag.Loai = loai;
        ViewBag.TongBanGhi = await _db.NhatKys.CountAsync();
        ViewBag.TongChiTiet = await _db.NhatKyChiTiets.CountAsync();
        return View(await query.OrderByDescending(x => x.ThoiGian).Take(500).ToListAsync());
    }

    [Authorize(Roles = "Admin"), Route("quan-tri/nhat-ky-chi-tiet")]
    public async Task<IActionResult> NhatKyChiTiet(string? q, string? phuongThuc, int trang = 1)
    {
        trang = Math.Max(1, trang);
        var query = _db.NhatKyChiTiets.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.NguoiDung.Contains(q) || x.HanhDong.Contains(q) || x.ChiTiet.Contains(q)
                || x.VaiTro.Contains(q) || x.KetQua.Contains(q) || x.DuongDan.Contains(q) || x.TraceId.Contains(q) || x.DiaChiIP.Contains(q));
        if (!string.IsNullOrWhiteSpace(phuongThuc)) query = query.Where(x => x.PhuongThuc == phuongThuc);
        ViewBag.Q = q; ViewBag.PhuongThuc = phuongThuc;
        ViewBag.Trang = trang;
        ViewBag.TongBanGhi = await query.CountAsync();
        return View(await query.OrderByDescending(x => x.ThoiGian).Skip((trang - 1) * 200).Take(200).ToListAsync());
    }
    private static readonly string[] CacKhoiTrangChu = { "hero", "banner", "danhmuc", "banchay", "moimua", "noibat", "xephang", "ket" };

    private static string ChuanHoaCauTruc(string? json)
    {
        try
        {
            var nhan = System.Text.Json.JsonSerializer.Deserialize<string[]>(json ?? "[]") ?? Array.Empty<string>();
            var daChon = nhan.Where(x => CacKhoiTrangChu.Contains(x, StringComparer.Ordinal)).Distinct(StringComparer.Ordinal).ToList();
            foreach (var ma in CacKhoiTrangChu) if (!daChon.Contains(ma)) daChon.Add(ma);
            return System.Text.Json.JsonSerializer.Serialize(daChon);
        }
        catch { return System.Text.Json.JsonSerializer.Serialize(CacKhoiTrangChu); }
    }

    private static string ChuanHoaKhoiAn(string? json)
    {
        try
        {
            var nhan = System.Text.Json.JsonSerializer.Deserialize<string[]>(json ?? "[]") ?? Array.Empty<string>();
            var daAn = nhan.Where(x => CacKhoiTrangChu.Contains(x, StringComparer.Ordinal)).Distinct(StringComparer.Ordinal).ToList();
            if (daAn.Count >= CacKhoiTrangChu.Length) daAn.Remove(CacKhoiTrangChu[0]);
            return System.Text.Json.JsonSerializer.Serialize(daAn);
        }
        catch { return "[]"; }
    }

    private static string MauHex(string? mau, string macDinh)
        => System.Text.RegularExpressions.Regex.IsMatch(mau ?? "", "^#[0-9a-fA-F]{6}$") ? mau! : macDinh;

    private static string Cat(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim()[..Math.Min(value.Trim().Length, 120)];
}
