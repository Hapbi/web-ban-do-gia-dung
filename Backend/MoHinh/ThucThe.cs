using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GIADUNGCHATLUONG.MoHinh;

// Các lớp trong tệp này mô tả dữ liệu của website.
// Entity là dữ liệu lưu trong bảng; các hằng trạng thái giúp tránh gõ sai giá trị.

public class NguoiDung
{
    public int Id { get; set; }
    [Required] public string Email { get; set; } = "";
    public string MatKhauHash { get; set; } = "";
    public string HoTen { get; set; } = "";
    public string DienThoai { get; set; } = "";
    public string VaiTro { get; set; } = global::GIADUNGCHATLUONG.MoHinh.VaiTro.KhachHang;
    public string Quyen { get; set; } = "";
    public bool BiKhoa { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.Now;
}

public static class VaiTro
{
    public const string Admin     = "Admin";
    public const string NhanVien  = "NhanVien";
    public const string AdminPhu  = "AdminPhu";
    public const string KhachHang = "KhachHang";
    public static string Ten(string v) => v switch
    {
        Admin     => "Admin lớn",
        NhanVien  => "Nhân viên",
        AdminPhu  => "Admin phụ",
        _         => "Khách hàng"
    };
}

public class DanhMuc
{
    public int Id { get; set; }
    [Required] public string Ten { get; set; } = "";
    public string Icon { get; set; } = "📦";
    public List<SanPham> DanhSachSanPham { get; set; } = new();
}

public class SanPham
{
    public int Id { get; set; }
    [Required] public string Ten { get; set; } = "";
    // Mô tả dài, công dụng và tính năng được lưu riêng để nội dung không bị dồn vào một ô.
    public string MoTa { get; set; } = "";
    public string CongDungChinh { get; set; } = "";
    public string DoiTuongSuDung { get; set; } = "";
    public string TinhNangNoiBat { get; set; } = "";
    public string LuuYSuDung { get; set; } = "";
    // Mỗi dòng là một thông số riêng, ví dụ: Công suất: 700W.
    public string ThongSo { get; set; } = "";
    public decimal GiaGoc { get; set; }
    public decimal? GiaKhuyenMai { get; set; }
    public int TonKho { get; set; }
    public int DaBan { get; set; }
    public string AnhChinh { get; set; } = "";
    public int DanhMucId { get; set; }
    public DanhMuc DanhMuc { get; set; }
    public bool DangBan { get; set; } = true;
    public bool NoiBat { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public List<DanhGia> DanhSachDanhGia { get; set; } = new();

    [NotMapped] public decimal GiaHienTai => GiaKhuyenMai.HasValue && GiaGoc > 0 && GiaKhuyenMai < GiaGoc ? GiaKhuyenMai.Value : GiaGoc;
    [NotMapped] public bool CoGiamGia => GiaGoc > 0 && GiaHienTai < GiaGoc;
    [NotMapped] public int PhanTramGiam => CoGiamGia ? (int)Math.Round((1 - (double)(GiaHienTai / GiaGoc)) * 100) : 0;
    [NotMapped] public double DiemTrungBinh
    {
        get
        {
            var list = DanhSachDanhGia.Where(x => x.DaDuyet).ToList();
            return list.Count == 0 ? 0 : list.Average(x => x.SoSao);
        }
    }
    [NotMapped] public int SoDanhGia => DanhSachDanhGia.Count(x => x.DaDuyet);
}

public class DanhGia
{
    public int Id { get; set; }
    public int SanPhamId { get; set; }
    public SanPham SanPham { get; set; }
    public int NguoiDungId { get; set; }
    public NguoiDung NguoiDung { get; set; }
    public int SoSao { get; set; } = 5;
    public string TieuDe { get; set; } = "";
    public string NoiDung { get; set; } = "";
    public bool DaDuyet { get; set; } = true;
    public DateTime NgayTao { get; set; } = DateTime.Now;
    [NotMapped] public bool DaMuaXacMinh { get; set; }
}

public class YeuThich
{
    public int Id { get; set; }
    public int NguoiDungId { get; set; }
    public int SanPhamId { get; set; }
    public SanPham SanPham { get; set; }
}

public class DongGioHang
{
    public SanPham SanPham { get; set; }
    public int SoLuong { get; set; }
    public decimal ThanhTien => SanPham.GiaHienTai * SoLuong;
}

public class DonHang
{
    public int Id { get; set; }
    public string MaDon { get; set; } = "";
    public int? NguoiDungId { get; set; }
    public NguoiDung NguoiDung { get; set; }
    public string HoTen { get; set; } = "";
    public string DienThoai { get; set; } = "";
    public string DiaChi { get; set; } = "";
    public string GhiChu { get; set; } = "";
    public decimal TongTien { get; set; }
    public string TrangThai { get; set; } = global::GIADUNGCHATLUONG.MoHinh.TrangThaiDon.ChoXacNhan;
    public string PhuongThucThanhToan { get; set; } = global::GIADUNGCHATLUONG.MoHinh.PhuongThucThanhToan.COD;
    public string ThanhToan { get; set; } = global::GIADUNGCHATLUONG.MoHinh.ThanhToan.ChuaThanhToan;
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public List<ChiTietDonHang> ChiTiet { get; set; } = new();
}

public class ChiTietDonHang
{
    public int Id { get; set; }
    public int DonHangId { get; set; }
    public int SanPhamId { get; set; }
    public string TenSanPham { get; set; } = "";
    public string AnhSanPham { get; set; } = "";
    public decimal DonGia { get; set; }
    public int SoLuong { get; set; }
    public decimal ThanhTien => DonGia * SoLuong;
}

public static class TrangThaiDon
{
    public const string ChoXacNhan = "ChoXacNhan";
    public const string DaXacNhan  = "DaXacNhan";
    public const string DangGiao   = "DangGiao";
    public const string HoanThanh  = "HoanThanh";
    public const string DaHuy      = "DaHuy";
    public static readonly string[] TatCa = { ChoXacNhan, DaXacNhan, DangGiao, HoanThanh, DaHuy };
    public static string Ten(string s) => s switch
    {
        ChoXacNhan => "Chờ xác nhận",
        DaXacNhan  => "Đã xác nhận",
        DangGiao   => "Đang giao",
        HoanThanh  => "Hoàn thành",
        DaHuy      => "Đã hủy",
        _          => s
    };
    public static string Mau(string s) => s switch
    {
        ChoXacNhan => "cam",
        DaXacNhan  => "xanh",
        DangGiao   => "tim",
        HoanThanh  => "luc",
        DaHuy      => "do",
        _          => "xam"
    };
}

public static class PhuongThucThanhToan
{
    public const string COD = "COD";
    public const string ChuyenKhoan = "ChuyenKhoan";
    public static string Ten(string s) => s == ChuyenKhoan ? "Chuyển khoản ngân hàng" : "Thanh toán khi nhận hàng (COD)";
}

public static class ThanhToan
{
    public const string ChuaThanhToan = "ChuaThanhToan";
    public const string DaThanhToan   = "DaThanhToan";
    public static string Ten(string s) => s == DaThanhToan ? "Đã thanh toán" : "Chưa thanh toán";
}

public class Banner
{
    public int Id { get; set; }
    public string TieuDe { get; set; } = "";
    public string AnhUrl { get; set; } = "";
    public string LienKet { get; set; } = "";
    public int ThuTu { get; set; }
    public bool DangHienThi { get; set; } = true;
}

public class BaiViet
{
    public int Id { get; set; }
    [Required] public string TieuDe { get; set; } = "";
    public string TomTat { get; set; } = "";
    public string NoiDung { get; set; } = "";
    public string AnhBia { get; set; } = "";
    public string TacGia { get; set; } = "";
    public bool DaDang { get; set; } = true;
    public DateTime NgayTao { get; set; } = DateTime.Now;
}

public class LienHe
{
    public int Id { get; set; }
    public string HoTen { get; set; } = "";
    public string Email { get; set; } = "";
    public string DienThoai { get; set; } = "";
    public string NoiDung { get; set; } = "";
    public string TrangThai { get; set; } = "Moi";
    public string GhiChuXuLy { get; set; } = "";
    public DateTime NgayTao { get; set; } = DateTime.Now;
}

public class CauHinh
{
    public int Id { get; set; }
    public string TenShop { get; set; } = "GIADUNGCHATLUONG";
    public string Slogan { get; set; } = "Đồ gia dụng chính hãng cho mọi nhà";
    public string DiaChi { get; set; } = "";
    public string DienThoai { get; set; } = "";
    public string Email { get; set; } = "";
    public string GioMoCua { get; set; } = "8:00 - 21:00";
    public string NganHang { get; set; } = "";
    public string SoTaiKhoan { get; set; } = "";
    public string ChuTaiKhoan { get; set; } = "";
    public string MaQr { get; set; } = "";
    public string MauChuDao { get; set; } = "#2563EB";
    public string MauPhu { get; set; } = "#0F766E";
    public string MauNen { get; set; } = "#F7F8FC";
    public string MauChu { get; set; } = "#182235";
    public string MauThe { get; set; } = "#FFFFFF";
    public string KieuBoCuc { get; set; } = "rong";
    public string KieuChu { get; set; } = "system";
    public string CoChu { get; set; } = "binhthuong";
    public int DoRongTrang { get; set; } = 1400;
    public int SoCotSanPham { get; set; } = 4;
    public string KieuThe { get; set; } = "mem";
    public string KieuBong { get; set; } = "nhe";
    public string CauTrucTrangChu { get; set; } = "[\"hero\",\"banner\",\"danhmuc\",\"banchay\",\"moimua\",\"noibat\",\"xephang\",\"ket\"]";
    public string CacKhoiAn { get; set; } = "[]";
    public string QuaHangNhat { get; set; } = "Voucher mua sắm 500.000đ";
    public string QuaHangNhi { get; set; } = "Voucher mua sắm 300.000đ";
    public string QuaHangBa { get; set; } = "Voucher mua sắm 150.000đ";
}

public class NhatKy
{
    public int Id { get; set; }
    public DateTime ThoiGian { get; set; } = DateTime.Now;
    public string NguoiDung { get; set; } = "";
    public string HanhDong { get; set; } = "";
    public string ChiTiet { get; set; } = "";
    public int? NguoiDungId { get; set; }
    public string VaiTro { get; set; } = "";
    public string Loai { get; set; } = "";
    public string KetQua { get; set; } = "Thành công";
    public string DiaChiIP { get; set; } = "";
    public string DuongDan { get; set; } = "";
    public string TraceId { get; set; } = "";
}


/// <summary>Nhật ký kỹ thuật: lưu từng yêu cầu để tra cứu khi cần.</summary>
public class NhatKyChiTiet
{
    public int Id { get; set; }
    public DateTime ThoiGian { get; set; } = DateTime.Now;
    public string NguoiDung { get; set; } = "";
    public int? NguoiDungId { get; set; }
    public string VaiTro { get; set; } = "";
    public string HanhDong { get; set; } = "";
    public string PhuongThuc { get; set; } = "GET";
    public string DuongDan { get; set; } = "";
    public string KetQua { get; set; } = "Thành công";
    public int MaTrangThai { get; set; }
    public string DiaChiIP { get; set; } = "";
    public string TraceId { get; set; } = "";
    public string ChiTiet { get; set; } = "";
}
