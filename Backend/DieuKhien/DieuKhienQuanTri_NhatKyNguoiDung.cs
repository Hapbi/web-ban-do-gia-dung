using GIADUNGCHATLUONG.MoHinh;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GIADUNGCHATLUONG.DieuKhien;

/// <summary>
/// API nhật ký theo từng tài khoản. Chỉ Admin được xem dữ liệu hoạt động.
/// Phạm vi dữ liệu chỉ gồm các yêu cầu và sự kiện được ghi nhận bên trong website.
/// </summary>
public partial class QuanTriController
{
    [HttpGet("/quan-tri/nhat-ky-theo-nguoi")]
    [Authorize(Roles = "Admin")]
    public IActionResult NhatKyTheoNguoi()
    {
        var duongDanTep = Path.Combine(_env.WebRootPath, "HTML", "quan-tri", "nhat-ky-nguoi-dung.html");
        return PhysicalFile(duongDanTep, "text/html; charset=utf-8");
    }

    [HttpGet("/api/quan-tri/nhat-ky/nguoi-dung")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApiDanhSachNguoiDungNhatKy(
        [FromQuery] string? q,
        [FromQuery] string? vaiTro,
        [FromQuery] DateTime? tuNgay,
        [FromQuery] DateTime? denNgay)
    {
        var nguoiDungQuery = _db.NguoiDungs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var tuKhoa = q.Trim();
            nguoiDungQuery = nguoiDungQuery.Where(x => x.HoTen.Contains(tuKhoa) || x.Email.Contains(tuKhoa));
        }
        if (!string.IsNullOrWhiteSpace(vaiTro))
            nguoiDungQuery = nguoiDungQuery.Where(x => x.VaiTro == vaiTro);

        var nguoiDungs = await nguoiDungQuery
            .OrderBy(x => x.HoTen)
            .Select(x => new
            {
                x.Id,
                x.HoTen,
                x.Email,
                x.VaiTro,
                x.BiKhoa,
                x.NgayTao
            })
            .ToListAsync();

        var ids = nguoiDungs.Select(x => x.Id).ToList();
        if (ids.Count == 0)
            return Json(new { items = Array.Empty<object>(), tongNguoi = 0, tongSuKien = 0, tongRequest = 0 });

        var tu = tuNgay?.Date;
        var den = denNgay?.Date.AddDays(1);
        var nghiepVuQuery = _db.NhatKys.AsNoTracking()
            .Where(x => x.NguoiDungId.HasValue && ids.Contains(x.NguoiDungId.Value));
        var chiTietQuery = _db.NhatKyChiTiets.AsNoTracking()
            .Where(x => x.NguoiDungId.HasValue && ids.Contains(x.NguoiDungId.Value));

        if (tu.HasValue)
        {
            nghiepVuQuery = nghiepVuQuery.Where(x => x.ThoiGian >= tu.Value);
            chiTietQuery = chiTietQuery.Where(x => x.ThoiGian >= tu.Value);
        }
        if (den.HasValue)
        {
            nghiepVuQuery = nghiepVuQuery.Where(x => x.ThoiGian < den.Value);
            chiTietQuery = chiTietQuery.Where(x => x.ThoiGian < den.Value);
        }

        var tongHopNghiepVu = await nghiepVuQuery
            .GroupBy(x => x.NguoiDungId)
            .Select(g => new { Id = g.Key, SoLuong = g.Count(), LanCuoi = g.Max(x => x.ThoiGian) })
            .ToListAsync();
        var tongHopChiTiet = await chiTietQuery
            .GroupBy(x => x.NguoiDungId)
            .Select(g => new { Id = g.Key, SoLuong = g.Count(), LanCuoi = g.Max(x => x.ThoiGian) })
            .ToListAsync();

        var nghiepVuMap = tongHopNghiepVu.ToDictionary(x => x.Id!.Value);
        var chiTietMap = tongHopChiTiet.ToDictionary(x => x.Id!.Value);

        var items = nguoiDungs.Select(user =>
        {
            var soNghiepVu = nghiepVuMap.TryGetValue(user.Id, out var nv) ? nv.SoLuong : 0;
            var soChiTiet = chiTietMap.TryGetValue(user.Id, out var ct) ? ct.SoLuong : 0;
            DateTime? hoatDongCuoi = nv?.LanCuoi;
            if (ct != null && (!hoatDongCuoi.HasValue || ct.LanCuoi > hoatDongCuoi.Value))
                hoatDongCuoi = ct.LanCuoi;

            return new
            {
                user.Id,
                user.HoTen,
                user.Email,
                user.VaiTro,
                TenVaiTro = VaiTro.Ten(user.VaiTro),
                user.BiKhoa,
                user.NgayTao,
                SoSuKienQuanTrong = soNghiepVu,
                SoRequest = soChiTiet,
                HoatDongCuoi = hoatDongCuoi
            };
        })
        .OrderByDescending(x => x.HoatDongCuoi ?? DateTime.MinValue)
        .ThenBy(x => x.HoTen)
        .ToList();

        return Json(new
        {
            items,
            tongNguoi = items.Count,
            tongSuKien = items.Sum(x => x.SoSuKienQuanTrong),
            tongRequest = items.Sum(x => x.SoRequest)
        });
    }

    [HttpGet("/api/quan-tri/nhat-ky/nguoi-dung/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApiChiTietNhatKyNguoiDung(
        int id,
        [FromQuery] string? q,
        [FromQuery] string? loai,
        [FromQuery] DateTime? tuNgay,
        [FromQuery] DateTime? denNgay,
        [FromQuery] int trang = 1,
        [FromQuery] int kichThuoc = 40)
    {
        var user = await _db.NguoiDungs.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.HoTen,
                x.Email,
                x.VaiTro,
                x.BiKhoa,
                x.NgayTao
            })
            .FirstOrDefaultAsync();

        if (user == null) return NotFound(new { message = "Không tìm thấy tài khoản." });

        trang = Math.Clamp(trang, 1, 1000);
        kichThuoc = Math.Clamp(kichThuoc, 10, 100);
        var tu = tuNgay?.Date;
        var den = denNgay?.Date.AddDays(1);

        var nghiepVuQuery = _db.NhatKys.AsNoTracking().Where(x => x.NguoiDungId == id);
        var chiTietQuery = _db.NhatKyChiTiets.AsNoTracking().Where(x => x.NguoiDungId == id);

        if (tu.HasValue)
        {
            nghiepVuQuery = nghiepVuQuery.Where(x => x.ThoiGian >= tu.Value);
            chiTietQuery = chiTietQuery.Where(x => x.ThoiGian >= tu.Value);
        }
        if (den.HasValue)
        {
            nghiepVuQuery = nghiepVuQuery.Where(x => x.ThoiGian < den.Value);
            chiTietQuery = chiTietQuery.Where(x => x.ThoiGian < den.Value);
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            var tuKhoa = q.Trim();
            nghiepVuQuery = nghiepVuQuery.Where(x => x.HanhDong.Contains(tuKhoa)
                || x.ChiTiet.Contains(tuKhoa) || x.Loai.Contains(tuKhoa) || x.KetQua.Contains(tuKhoa));
            chiTietQuery = chiTietQuery.Where(x => x.HanhDong.Contains(tuKhoa)
                || x.ChiTiet.Contains(tuKhoa) || x.DuongDan.Contains(tuKhoa)
                || x.TraceId.Contains(tuKhoa) || x.KetQua.Contains(tuKhoa));
        }

        var chiXemNghiepVu = string.Equals(loai, "nghiep-vu", StringComparison.OrdinalIgnoreCase);
        var chiXemChiTiet = string.Equals(loai, "chi-tiet", StringComparison.OrdinalIgnoreCase);
        var chiXemLoi = string.Equals(loai, "that-bai", StringComparison.OrdinalIgnoreCase);
        if (chiXemNghiepVu) chiTietQuery = chiTietQuery.Where(_ => false);
        if (chiXemChiTiet) nghiepVuQuery = nghiepVuQuery.Where(_ => false);
        if (chiXemLoi)
        {
            nghiepVuQuery = nghiepVuQuery.Where(x => x.KetQua != "Thành công");
            chiTietQuery = chiTietQuery.Where(x => x.MaTrangThai >= 400 || x.KetQua != "Thành công");
        }

        var tongNghiepVu = chiXemChiTiet ? 0 : await nghiepVuQuery.CountAsync();
        var tongChiTiet = chiXemNghiepVu ? 0 : await chiTietQuery.CountAsync();
        var canLay = trang * kichThuoc + 1;

        var nghiepVu = chiXemChiTiet
            ? new List<NhatKy>()
            : await nghiepVuQuery.OrderByDescending(x => x.ThoiGian).Take(canLay).ToListAsync();
        var chiTiet = chiXemNghiepVu
            ? new List<NhatKyChiTiet>()
            : await chiTietQuery.OrderByDescending(x => x.ThoiGian).Take(canLay).ToListAsync();

        var timeline = nghiepVu.Select(x => new NhatKyTheoNguoiItem
        {
            Id = x.Id,
            ThoiGian = x.ThoiGian,
            Nguon = "nghiep-vu",
            Nhan = x.HanhDong,
            Nhom = x.Loai,
            ChiTiet = x.ChiTiet,
            KetQua = x.KetQua,
            DuongDan = x.DuongDan,
            DiaChiIP = x.DiaChiIP,
            TraceId = x.TraceId,
            QuanTrong = true
        })
        .Concat(chiTiet.Select(x => new NhatKyTheoNguoiItem
        {
            Id = x.Id,
            ThoiGian = x.ThoiGian,
            Nguon = "chi-tiet",
            Nhan = x.HanhDong,
            Nhom = x.PhuongThuc,
            ChiTiet = x.ChiTiet,
            KetQua = x.KetQua,
            PhuongThuc = x.PhuongThuc,
            DuongDan = x.DuongDan,
            MaTrangThai = x.MaTrangThai,
            DiaChiIP = x.DiaChiIP,
            TraceId = x.TraceId,
            QuanTrong = false
        }))
        .OrderByDescending(x => x.ThoiGian)
        .ToList();

        var tong = tongNghiepVu + tongChiTiet;
        var phanTrang = timeline.Skip((trang - 1) * kichThuoc).Take(kichThuoc).ToList();

        // Biểu đồ độc lập với bộ lọc bảng: cho biết nhịp hoạt động 14 ngày gần nhất.
        var ngayBatDauBieuDo = DateTime.Today.AddDays(-13);
        var ngayKetThucBieuDo = DateTime.Today.AddDays(1);
        var mocNghiepVu = await _db.NhatKys.AsNoTracking()
            .Where(x => x.NguoiDungId == id && x.ThoiGian >= ngayBatDauBieuDo && x.ThoiGian < ngayKetThucBieuDo)
            .Select(x => x.ThoiGian).ToListAsync();
        var mocChiTiet = await _db.NhatKyChiTiets.AsNoTracking()
            .Where(x => x.NguoiDungId == id && x.ThoiGian >= ngayBatDauBieuDo && x.ThoiGian < ngayKetThucBieuDo)
            .Select(x => x.ThoiGian).ToListAsync();

        var bieuDo = Enumerable.Range(0, 14).Select(i =>
        {
            var ngay = ngayBatDauBieuDo.AddDays(i);
            return new
            {
                Ngay = ngay.ToString("dd/MM"),
                NghiepVu = mocNghiepVu.Count(x => x.Date == ngay),
                TruyCap = mocChiTiet.Count(x => x.Date == ngay)
            };
        }).ToList();

        var tatCaNghiepVu = await _db.NhatKys.AsNoTracking().CountAsync(x => x.NguoiDungId == id);
        var tatCaChiTiet = await _db.NhatKyChiTiets.AsNoTracking().CountAsync(x => x.NguoiDungId == id);
        var loi = await _db.NhatKyChiTiets.AsNoTracking()
            .CountAsync(x => x.NguoiDungId == id && x.MaTrangThai >= 400);

        return Json(new
        {
            user = new
            {
                user.Id,
                user.HoTen,
                user.Email,
                user.VaiTro,
                TenVaiTro = VaiTro.Ten(user.VaiTro),
                user.BiKhoa,
                user.NgayTao
            },
            tongNghiepVu = tatCaNghiepVu,
            tongRequest = tatCaChiTiet,
            tongLoi = loi,
            tongKetQuaLoc = tong,
            trang,
            kichThuoc,
            conTrangSau = tong > trang * kichThuoc,
            bieuDo,
            items = phanTrang
        });
    }

    private sealed class NhatKyTheoNguoiItem
    {
        public int Id { get; set; }
        public DateTime ThoiGian { get; set; }
        public string Nguon { get; set; } = "";
        public string Nhan { get; set; } = "";
        public string Nhom { get; set; } = "";
        public string ChiTiet { get; set; } = "";
        public string KetQua { get; set; } = "";
        public string PhuongThuc { get; set; } = "";
        public string DuongDan { get; set; } = "";
        public int MaTrangThai { get; set; }
        public string DiaChiIP { get; set; } = "";
        public string TraceId { get; set; } = "";
        public bool QuanTrong { get; set; }
    }
}
