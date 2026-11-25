using GIADUNGCHATLUONG.MoHinh;
using Microsoft.EntityFrameworkCore;

namespace GIADUNGCHATLUONG.DuLieu;

/// <summary>
/// Lớp kết nối giữa chương trình C# và cơ sở dữ liệu.
/// Mỗi DbSet bên dưới tương ứng với một bảng dữ liệu.
/// </summary>
public class AppDb : DbContext
{
    // Nhận cấu hình kết nối do Program.cs đăng ký và truyền cho Entity Framework Core.
    public AppDb(DbContextOptions<AppDb> tuyChonKetNoi) : base(tuyChonKetNoi) { }

    // Danh sách các bảng mà website sử dụng.

    public DbSet<NguoiDung>      NguoiDungs      => Set<NguoiDung>();
    public DbSet<DanhMuc>        DanhMucs        => Set<DanhMuc>();
    public DbSet<SanPham>        SanPhams        => Set<SanPham>();
    public DbSet<DanhGia>        DanhGias        => Set<DanhGia>();
    public DbSet<YeuThich>       YeuThichs       => Set<YeuThich>();
    public DbSet<DonHang>        DonHangs        => Set<DonHang>();
    public DbSet<ChiTietDonHang> ChiTietDonHangs => Set<ChiTietDonHang>();
    public DbSet<Banner>         Banners         => Set<Banner>();
    public DbSet<BaiViet>        BaiViets        => Set<BaiViet>();
    public DbSet<LienHe>         LienHes         => Set<LienHe>();
    public DbSet<CauHinh>        CauHinhs        => Set<CauHinh>();
    public DbSet<NhatKy>         NhatKys         => Set<NhatKy>();
    public DbSet<NhatKyChiTiet>  NhatKyChiTiets  => Set<NhatKyChiTiet>();

    // Khai báo độ dài, chỉ mục và mối quan hệ giữa các bảng.
    protected override void OnModelCreating(ModelBuilder boTaoMoHinh)
    {
        boTaoMoHinh.Entity<SanPham>().Property(x => x.GiaGoc).HasPrecision(18, 2);
        boTaoMoHinh.Entity<SanPham>().Property(x => x.GiaKhuyenMai).HasPrecision(18, 2);
        boTaoMoHinh.Entity<DonHang>().Property(x => x.TongTien).HasPrecision(18, 2);
        boTaoMoHinh.Entity<ChiTietDonHang>().Property(x => x.DonGia).HasPrecision(18, 2);

        // SQL Server không cho lập chỉ mục trực tiếp trên nvarchar(max).
        // Giới hạn 450 ký tự vừa đủ rộng, vừa tương thích giới hạn khóa chỉ mục.
        boTaoMoHinh.Entity<NguoiDung>().Property(x => x.Email).HasMaxLength(450).IsRequired();
        boTaoMoHinh.Entity<DonHang>().Property(x => x.MaDon).HasMaxLength(450).IsRequired();

        boTaoMoHinh.Entity<NguoiDung>().HasIndex(x => x.Email).IsUnique();
        boTaoMoHinh.Entity<DonHang>().HasIndex(x => x.MaDon).IsUnique();
        boTaoMoHinh.Entity<DonHang>().HasIndex(x => new { x.NguoiDungId, x.NgayTao });
        boTaoMoHinh.Entity<NhatKy>().HasIndex(x => x.ThoiGian);
        boTaoMoHinh.Entity<NhatKyChiTiet>().HasIndex(x => x.ThoiGian);
        boTaoMoHinh.Entity<NhatKyChiTiet>().HasIndex(x => new { x.NguoiDungId, x.ThoiGian });
        boTaoMoHinh.Entity<DonHang>()
            .HasOne(x => x.NguoiDung).WithMany()
            .HasForeignKey(x => x.NguoiDungId).OnDelete(DeleteBehavior.SetNull);
        boTaoMoHinh.Entity<ChiTietDonHang>().HasOne<DonHang>()
            .WithMany(x => x.ChiTiet).HasForeignKey(x => x.DonHangId)
            .OnDelete(DeleteBehavior.Cascade);
        boTaoMoHinh.Entity<SanPham>().HasOne(x => x.DanhMuc)
            .WithMany(x => x.DanhSachSanPham).HasForeignKey(x => x.DanhMucId)
            .OnDelete(DeleteBehavior.Restrict);
        boTaoMoHinh.Entity<DanhGia>().HasOne(x => x.SanPham)
            .WithMany(x => x.DanhSachDanhGia).HasForeignKey(x => x.SanPhamId)
            .OnDelete(DeleteBehavior.Cascade);
        boTaoMoHinh.Entity<DanhGia>().HasOne(x => x.NguoiDung)
            .WithMany().HasForeignKey(x => x.NguoiDungId)
            .OnDelete(DeleteBehavior.Cascade);
        boTaoMoHinh.Entity<DanhGia>().HasIndex(x => new { x.NguoiDungId, x.SanPhamId }).IsUnique();
        boTaoMoHinh.Entity<YeuThich>().HasIndex(x => new { x.NguoiDungId, x.SanPhamId }).IsUnique();
        boTaoMoHinh.Entity<YeuThich>().HasOne<NguoiDung>().WithMany()
            .HasForeignKey(x => x.NguoiDungId).OnDelete(DeleteBehavior.Cascade);
        boTaoMoHinh.Entity<YeuThich>().HasOne(x => x.SanPham)
            .WithMany().HasForeignKey(x => x.SanPhamId).OnDelete(DeleteBehavior.Cascade);
    }
}
