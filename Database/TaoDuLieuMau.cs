using GIADUNGCHATLUONG.TienIch;
using GIADUNGCHATLUONG.MoHinh;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace GIADUNGCHATLUONG.DuLieu;

/// <summary>
/// Tạo cấu trúc bảng và dữ liệu mẫu khi website chạy lần đầu.
/// Các bước kiểm tra AnyAsync giúp tránh thêm dữ liệu mẫu lặp lại ở mỗi lần khởi động.
/// </summary>
public static class KhoiTaoDuLieu
{
    public static async Task KhoiTaoAsync(AppDb coSoDuLieu)
    {
        // Bước 1: tạo bảng nếu chưa có và bổ sung cấu trúc mới cho cơ sở dữ liệu cũ.

        await coSoDuLieu.Database.EnsureCreatedAsync();
        await DamBaoCotMoiAsync(coSoDuLieu);

        // Bước 2: tạo cấu hình cửa hàng mặc định nếu bảng cấu hình đang trống.
        if (!await coSoDuLieu.CauHinhs.AnyAsync())
        {
            coSoDuLieu.CauHinhs.Add(new CauHinh
            {
                TenShop     = "GIADUNGCHATLUONG",
                Slogan      = "Đồ gia dụng chính hãng cho mọi nhà",
                DiaChi      = "123 Đường Nguyễn Trãi, Thanh Xuân, Hà Nội",
                DienThoai   = "0855 081 828",
                Email       = "lienhe@example.com",
                GioMoCua    = "8:00 - 21:00 hằng ngày",
                NganHang    = "Vietcombank",
                SoTaiKhoan  = "0855081828",
                ChuTaiKhoan = "NGUYEN VAN A"
            });
            await coSoDuLieu.SaveChangesAsync();
        }

        // Bước 3: tạo tài khoản mẫu để đăng nhập thử các vai trò.
        if (!await coSoDuLieu.NguoiDungs.AnyAsync())
        {
            coSoDuLieu.NguoiDungs.AddRange(
                new NguoiDung { Email = "admin@shop.vn", HoTen = "Quản Trị Viên", DienThoai = "0900000001",
                                VaiTro = VaiTro.Admin, MatKhauHash = MatKhauHelper.Hash("Admin@123") },
                new NguoiDung { Email = "staff@shop.vn", HoTen = "Nhân Viên", DienThoai = "0900000002",
                                VaiTro = VaiTro.NhanVien, Quyen = "sanpham,donhang,baocao",
                                MatKhauHash = MatKhauHelper.Hash("Staff@123") },
                new NguoiDung { Email = "khach@shop.vn", HoTen = "Khách Hàng", DienThoai = "0901234567",
                                VaiTro = VaiTro.KhachHang, MatKhauHash = MatKhauHelper.Hash("Khach@123") }
            );
            await coSoDuLieu.SaveChangesAsync();
        }

        // Bước 4: tạo danh mục và sản phẩm mẫu nếu chưa có dữ liệu.
        if (!await coSoDuLieu.DanhMucs.AnyAsync())
        {
            coSoDuLieu.DanhMucs.AddRange(
                new DanhMuc { Ten = "Nhà bếp",         Icon = "🍳" },
                new DanhMuc { Ten = "Điện gia dụng",   Icon = "🔌" },
                new DanhMuc { Ten = "Vệ sinh nhà cửa", Icon = "🧹" },
                new DanhMuc { Ten = "Phòng ngủ",       Icon = "🛏️" },
                new DanhMuc { Ten = "Điện cầm tay",    Icon = "🔧" },
                new DanhMuc { Ten = "Đồ dùng khác",    Icon = "📦" }
            );
            await coSoDuLieu.SaveChangesAsync();
        }

        if (!await coSoDuLieu.SanPhams.AnyAsync())
        {
            var danhMucs = await coSoDuLieu.DanhMucs.OrderBy(x => x.Id).ToListAsync();
            var homNay = DateTime.Now;

            var dsSanPham = new List<SanPham>
            {
                new() { Ten = "Nồi cơm điện tử 1.8L", DanhMucId = danhMucs[0].Id, GiaGoc = 1290000, GiaKhuyenMai = 1090000, TonKho = 25, NoiBat = true,
                        MoTa = "Nồi cơm điện tử dung tích 1.8L, phù hợp gia đình 3-5 người.",
                        ThongSo = "Dung tích: 1.8L\nCông suất: 700W\nBảo hành: 24 tháng",
                        AnhChinh = "https://images.unsplash.com/photo-1584269600519-112d071b35e6?w=700",
                        NgayTao = homNay.AddDays(-30) },
                new() { Ten = "Ấm siêu tốc inox 1.7L", DanhMucId = danhMucs[0].Id, GiaGoc = 450000, TonKho = 32,
                        MoTa = "Ấm siêu tốc inox 304, tự ngắt khi sôi.",
                        ThongSo = "Dung tích: 1.7L\nChất liệu: Inox 304",
                        AnhChinh = "https://images.unsplash.com/photo-1585232004423-244e0e6904e3?w=700",
                        NgayTao = homNay.AddDays(-28) },
                new() { Ten = "Máy xay sinh tố đa năng", DanhMucId = danhMucs[0].Id, GiaGoc = 890000, GiaKhuyenMai = 790000, TonKho = 14, NoiBat = true,
                        MoTa = "Máy xay sinh tố 3 cối, xay đá và trái cây.",
                        ThongSo = "Công suất: 600W\nSố cối: 3",
                        AnhChinh = "https://images.unsplash.com/photo-1570222094114-d054a817e56b?w=700",
                        NgayTao = homNay.AddDays(-25) },
                new() { Ten = "Chảo chống dính 26cm", DanhMucId = danhMucs[0].Id, GiaGoc = 320000, TonKho = 40,
                        MoTa = "Chảo chống dính đường kính 26cm.",
                        ThongSo = "Đường kính: 26cm\nBề mặt: Chống dính",
                        AnhChinh = "https://images.unsplash.com/photo-1585515320310-259814833e62?w=700",
                        NgayTao = homNay.AddDays(-22) },
                new() { Ten = "Quạt đứng 5 cánh", DanhMucId = danhMucs[1].Id, GiaGoc = 1250000, GiaKhuyenMai = 1090000, TonKho = 18,
                        MoTa = "Quạt đứng 5 cánh, 3 tốc độ gió.",
                        ThongSo = "Số cánh: 5\nCông suất: 55W",
                        AnhChinh = "https://images.unsplash.com/photo-1595371685289-84b288247e2f?w=700",
                        NgayTao = homNay.AddDays(-20) },
                new() { Ten = "Nồi chiên không dầu 5L", DanhMucId = danhMucs[1].Id, GiaGoc = 2490000, GiaKhuyenMai = 2190000, TonKho = 9, NoiBat = true,
                        MoTa = "Nồi chiên không dầu 5L, công nghệ Rapid Air.",
                        ThongSo = "Dung tích: 5L\nCông suất: 1500W",
                        AnhChinh = "https://images.unsplash.com/photo-1626200419199-391ae4be7a41?w=700",
                        NgayTao = homNay.AddDays(-18) },
                new() { Ten = "Máy lọc không khí", DanhMucId = danhMucs[1].Id, GiaGoc = 2190000, TonKho = 7,
                        MoTa = "Máy lọc không khí cho phòng 30-40m².",
                        ThongSo = "Diện tích: 30-40m²\nMàng lọc: HEPA",
                        AnhChinh = "https://images.unsplash.com/photo-1585771724684-38269d6639fd?w=700",
                        NgayTao = homNay.AddDays(-15) },
                new() { Ten = "Bàn ủi hơi nước", DanhMucId = danhMucs[1].Id, GiaGoc = 650000, TonKho = 20,
                        MoTa = "Bàn ủi hơi nước cầm tay.",
                        ThongSo = "Công suất: 1800W\nBình nước: 300ml",
                        AnhChinh = "https://images.unsplash.com/photo-1585846416120-3a7354ed7d39?w=700",
                        NgayTao = homNay.AddDays(-12) },
                new() { Ten = "Máy hút bụi cầm tay", DanhMucId = danhMucs[2].Id, GiaGoc = 1590000, GiaKhuyenMai = 1390000, TonKho = 11, NoiBat = true,
                        MoTa = "Máy hút bụi cầm tay gọn nhẹ.",
                        ThongSo = "Công suất: 120W\nPin: 2500mAh",
                        AnhChinh = "https://images.unsplash.com/photo-1558317374-067fb5f30001?w=700",
                        NgayTao = homNay.AddDays(-10) },
                new() { Ten = "Robot hút bụi lau nhà", DanhMucId = danhMucs[2].Id, GiaGoc = 4990000, GiaKhuyenMai = 4290000, TonKho = 5, NoiBat = true,
                        MoTa = "Robot hút bụi và lau nhà tự động.",
                        ThongSo = "Pin: 5200mAh\nDiện tích: 250m²",
                        AnhChinh = "https://images.unsplash.com/photo-1589834390005-5d4fb9bf3d32?w=700",
                        NgayTao = homNay.AddDays(-8) },
                new() { Ten = "Bộ cây lau nhà 2 ngăn", DanhMucId = danhMucs[2].Id, GiaGoc = 390000, TonKho = 28,
                        MoTa = "Bộ cây lau nhà có thùng vắt 2 ngăn.",
                        ThongSo = "Chất liệu: Nhựa ABS + inox",
                        AnhChinh = "https://images.unsplash.com/photo-1585421514738-01798e348b17?w=700",
                        NgayTao = homNay.AddDays(-6) },
                new() { Ten = "Đèn bàn LED chống cận", DanhMucId = danhMucs[3].Id, GiaGoc = 280000, TonKho = 35,
                        MoTa = "Đèn bàn LED chống cận, 3 chế độ ánh sáng.",
                        ThongSo = "Công suất: 8W\nÁnh sáng: 3 chế độ",
                        AnhChinh = "https://images.unsplash.com/photo-1507473885765-e6ed057f782c?w=700",
                        NgayTao = homNay.AddDays(-4) },
                new() { Ten = "Máy sấy tóc 1800W", DanhMucId = danhMucs[3].Id, GiaGoc = 520000, GiaKhuyenMai = 450000, TonKho = 16,
                        MoTa = "Máy sấy tóc 1800W, 2 chế độ nhiệt.",
                        ThongSo = "Công suất: 1800W\nChế độ: 2 nhiệt + 2 gió",
                        AnhChinh = "https://images.unsplash.com/photo-1522338140262-f46f5913618a?w=700",
                        NgayTao = homNay.AddDays(-3) },
                new() { Ten = "Máy khoan cầm tay 750W", DanhMucId = danhMucs[4].Id, GiaGoc = 1290000, TonKho = 12,
                        MoTa = "Máy khoan cầm tay đa năng 750W.",
                        ThongSo = "Công suất: 750W\nTốc độ: 0-3000 rpm",
                        AnhChinh = "https://images.unsplash.com/photo-1504148455328-c376907d081c?w=700",
                        NgayTao = homNay.AddDays(-2) },
                new() { Ten = "Bộ tua vít 32 món", DanhMucId = danhMucs[4].Id, GiaGoc = 350000, TonKho = 50,
                        MoTa = "Bộ tua vít 32 món đa năng.",
                        ThongSo = "Số món: 32\nChất liệu: Thép CR-V",
                        AnhChinh = "https://images.unsplash.com/photo-1530124566582-a618bc2615dc?w=700",
                        NgayTao = homNay.AddDays(-1) },
                new() { Ten = "Hộp đựng thực phẩm 5 chiếc", DanhMucId = danhMucs[5].Id, GiaGoc = 180000, TonKho = 60, NoiBat = true,
                        MoTa = "Hộp đựng thực phẩm 5 chiếc, nhựa PP an toàn.",
                        ThongSo = "Số chiếc: 5\nChất liệu: Nhựa PP",
                        AnhChinh = "https://images.unsplash.com/photo-1584464491033-06628f3a6b7b?w=700",
                        NgayTao = homNay }
            };

            coSoDuLieu.SanPhams.AddRange(dsSanPham);
            await coSoDuLieu.SaveChangesAsync();
        }

        // Chỉnh danh mục sau khi đã có sản phẩm mẫu, cả lần tạo mới lẫn khi nâng cấp dữ liệu cũ.
        await DamBaoDanhMucChinhXacAsync(coSoDuLieu);

        if (!await coSoDuLieu.DanhGias.AnyAsync())
        {
            var khach = await coSoDuLieu.NguoiDungs.FirstOrDefaultAsync(x => x.Email == "khach@shop.vn");
            var sps   = await coSoDuLieu.SanPhams.Take(5).ToListAsync();
            if (khach != null && sps.Any())
            {
                coSoDuLieu.DanhGias.AddRange(
                    new DanhGia { SanPhamId = sps[0].Id, NguoiDungId = khach.Id, SoSao = 5, TieuDe = "Rất tốt", NoiDung = "Nồi cơm nấu ngon, giữ nóng tốt." },
                    new DanhGia { SanPhamId = sps[1].Id, NguoiDungId = khach.Id, SoSao = 4, TieuDe = "Ổn", NoiDung = "Chất lượng ổn trong tầm giá." },
                    new DanhGia { SanPhamId = sps[2].Id, NguoiDungId = khach.Id, SoSao = 5, TieuDe = "Đáng tiền", NoiDung = "Máy xay mạnh, xay đá tốt." }
                );
                await coSoDuLieu.SaveChangesAsync();
            }
        }

        if (!await coSoDuLieu.Banners.AnyAsync())
        {
            coSoDuLieu.Banners.AddRange(
                new Banner { TieuDe = "Ưu đãi mùa hè — giảm đến 30%", AnhUrl = "https://images.unsplash.com/photo-1556909114-f6e7ad7d3136?w=1600", ThuTu = 1, LienKet = "/san-pham" },
                new Banner { TieuDe = "Bộ sưu tập nhà bếp mới",       AnhUrl = "https://images.unsplash.com/photo-1556909212-d5b604d0c90d?w=1600", ThuTu = 2, LienKet = "/san-pham" }
            );
            await coSoDuLieu.SaveChangesAsync();
        }

        await BoSungTaiKhoanDemoAsync(coSoDuLieu);
        await TaoDuLieuMuaSamDemoAsync(coSoDuLieu);

        var cauHinh = await coSoDuLieu.CauHinhs.FirstOrDefaultAsync();
        // Chỉ đổi tên mặc định cũ; không ghi đè tên và slogan riêng mà cửa hàng đã tự cấu hình.
        if (cauHinh != null && cauHinh.TenShop == "GIADUNGTHAYGIANG")
        {
            cauHinh.TenShop = "GIADUNGCHATLUONG";
            await coSoDuLieu.SaveChangesAsync();
        }
    }

    /// <summary>Bổ sung nhóm hàng phù hợp và sửa lại ba mặt hàng mẫu đang nằm sai danh mục.</summary>
    private static async Task DamBaoDanhMucChinhXacAsync(AppDb coSoDuLieu)
    {
        var danhMucMoi = new[]
        {
            new DanhMuc { Ten = "Chăm sóc cá nhân", Icon = "🧴" },
            new DanhMuc { Ten = "Chiếu sáng", Icon = "💡" }
        };

        foreach (var danhMuc in danhMucMoi)
        {
            if (!await coSoDuLieu.DanhMucs.AnyAsync(x => x.Ten == danhMuc.Ten))
                coSoDuLieu.DanhMucs.Add(danhMuc);
        }
        await coSoDuLieu.SaveChangesAsync();

        var nhaBep = await coSoDuLieu.DanhMucs.FirstAsync(x => x.Ten == "Nhà bếp");
        var chamSoc = await coSoDuLieu.DanhMucs.FirstAsync(x => x.Ten == "Chăm sóc cá nhân");
        var chieuSang = await coSoDuLieu.DanhMucs.FirstAsync(x => x.Ten == "Chiếu sáng");

        var noiChien = await coSoDuLieu.SanPhams.FirstOrDefaultAsync(x => x.Ten == "Nồi chiên không dầu 5L");
        if (noiChien != null) noiChien.DanhMucId = nhaBep.Id;

        var maySayToc = await coSoDuLieu.SanPhams.FirstOrDefaultAsync(x => x.Ten == "Máy sấy tóc 1800W");
        if (maySayToc != null) maySayToc.DanhMucId = chamSoc.Id;

        var denBan = await coSoDuLieu.SanPhams.FirstOrDefaultAsync(x => x.Ten == "Đèn bàn LED chống cận");
        if (denBan != null) denBan.DanhMucId = chieuSang.Id;

        await coSoDuLieu.SaveChangesAsync();
    }

    /// <summary>Kiểm tra và bổ sung các cột/bảng mới cho dữ liệu cũ mà không tạo lại toàn bộ dữ liệu.</summary>
    private static async Task DamBaoCotMoiAsync(AppDb coSoDuLieu)
    {
        // SQLite cần bổ sung cột bằng ALTER TABLE; SQL Server có câu lệnh kiểm tra riêng.
        if (coSoDuLieu.Database.IsSqlite())
        {
            var cacLenh = new[]
            {
                "ALTER TABLE CauHinhs ADD COLUMN MauChuDao TEXT NOT NULL DEFAULT '#2563EB'",
                "ALTER TABLE CauHinhs ADD COLUMN MauPhu TEXT NOT NULL DEFAULT '#0F766E'",
                "ALTER TABLE CauHinhs ADD COLUMN MauNen TEXT NOT NULL DEFAULT '#F7F8FC'",
                "ALTER TABLE CauHinhs ADD COLUMN MauChu TEXT NOT NULL DEFAULT '#182235'",
                "ALTER TABLE CauHinhs ADD COLUMN MauThe TEXT NOT NULL DEFAULT '#FFFFFF'",
                "ALTER TABLE CauHinhs ADD COLUMN KieuBoCuc TEXT NOT NULL DEFAULT 'rong'",
                "ALTER TABLE CauHinhs ADD COLUMN KieuChu TEXT NOT NULL DEFAULT 'system'",
                "ALTER TABLE CauHinhs ADD COLUMN CoChu TEXT NOT NULL DEFAULT 'binhthuong'",
                "ALTER TABLE CauHinhs ADD COLUMN DoRongTrang INTEGER NOT NULL DEFAULT 1400",
                "ALTER TABLE CauHinhs ADD COLUMN SoCotSanPham INTEGER NOT NULL DEFAULT 4",
                "ALTER TABLE CauHinhs ADD COLUMN KieuThe TEXT NOT NULL DEFAULT 'mem'",
                "ALTER TABLE CauHinhs ADD COLUMN KieuBong TEXT NOT NULL DEFAULT 'nhe'",
                "ALTER TABLE CauHinhs ADD COLUMN CauTrucTrangChu TEXT NOT NULL DEFAULT '[\"hero\",\"banner\",\"danhmuc\",\"banchay\",\"moimua\",\"noibat\",\"xephang\",\"ket\"]'",
                "ALTER TABLE CauHinhs ADD COLUMN CacKhoiAn TEXT NOT NULL DEFAULT '[]'",
                "ALTER TABLE CauHinhs ADD COLUMN QuaHangNhat TEXT NOT NULL DEFAULT 'Voucher mua sắm 500.000đ'",
                "ALTER TABLE CauHinhs ADD COLUMN QuaHangNhi TEXT NOT NULL DEFAULT 'Voucher mua sắm 300.000đ'",
                "ALTER TABLE CauHinhs ADD COLUMN QuaHangBa TEXT NOT NULL DEFAULT 'Voucher mua sắm 150.000đ'",
                "ALTER TABLE SanPhams ADD COLUMN CongDungChinh TEXT NOT NULL DEFAULT ''",
                "ALTER TABLE SanPhams ADD COLUMN DoiTuongSuDung TEXT NOT NULL DEFAULT ''",
                "ALTER TABLE SanPhams ADD COLUMN TinhNangNoiBat TEXT NOT NULL DEFAULT ''",
                "ALTER TABLE SanPhams ADD COLUMN LuuYSuDung TEXT NOT NULL DEFAULT ''",
                "ALTER TABLE NhatKys ADD COLUMN NguoiDungId INTEGER NULL",
                "ALTER TABLE NhatKys ADD COLUMN VaiTro TEXT NOT NULL DEFAULT ''",
                "ALTER TABLE NhatKys ADD COLUMN Loai TEXT NOT NULL DEFAULT ''",
                "ALTER TABLE NhatKys ADD COLUMN KetQua TEXT NOT NULL DEFAULT 'Thành công'",
                "ALTER TABLE NhatKys ADD COLUMN DiaChiIP TEXT NOT NULL DEFAULT ''",
                "ALTER TABLE NhatKys ADD COLUMN DuongDan TEXT NOT NULL DEFAULT ''",
                "ALTER TABLE NhatKys ADD COLUMN TraceId TEXT NOT NULL DEFAULT ''",
                "CREATE INDEX IF NOT EXISTS IX_NhatKys_ThoiGian ON NhatKys (ThoiGian DESC)",
                "CREATE TABLE IF NOT EXISTS NhatKyChiTiets (Id INTEGER NOT NULL CONSTRAINT PK_NhatKyChiTiets PRIMARY KEY AUTOINCREMENT, ThoiGian TEXT NOT NULL, NguoiDung TEXT NOT NULL, NguoiDungId INTEGER NULL, VaiTro TEXT NOT NULL, HanhDong TEXT NOT NULL, PhuongThuc TEXT NOT NULL, DuongDan TEXT NOT NULL, KetQua TEXT NOT NULL, MaTrangThai INTEGER NOT NULL, DiaChiIP TEXT NOT NULL, TraceId TEXT NOT NULL, ChiTiet TEXT NOT NULL)",
                "CREATE INDEX IF NOT EXISTS IX_NhatKyChiTiets_ThoiGian ON NhatKyChiTiets (ThoiGian DESC)",
                "CREATE INDEX IF NOT EXISTS IX_NhatKyChiTiets_NguoiDungId_ThoiGian ON NhatKyChiTiets (NguoiDungId, ThoiGian DESC)"
            };

            foreach (var cauLenhSql in cacLenh)
            {
                try { await coSoDuLieu.Database.ExecuteSqlRawAsync(cauLenhSql); }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 1 && ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
            }
        }
        else if (coSoDuLieu.Database.IsSqlServer())
        {
            var cacLenh = new[]
            {
                // Sửa kiểu chuỗi cũ nếu trước đây được tạo thành nvarchar(max), để index duy nhất hoạt động.
                "IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.NguoiDungs') AND name = N'Email' AND max_length = -1) ALTER TABLE dbo.NguoiDungs ALTER COLUMN Email nvarchar(450) NOT NULL;",
                "IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.DonHangs') AND name = N'MaDon' AND max_length = -1) ALTER TABLE dbo.DonHangs ALTER COLUMN MaDon nvarchar(450) NOT NULL;",
                "IF COL_LENGTH(N'dbo.CauHinhs', N'MauChuDao') IS NULL ALTER TABLE dbo.CauHinhs ADD MauChuDao nvarchar(max) NOT NULL CONSTRAINT DF_CauHinhs_MauChuDao DEFAULT N'#2563EB';",
                "IF COL_LENGTH(N'dbo.CauHinhs', N'MauPhu') IS NULL ALTER TABLE dbo.CauHinhs ADD MauPhu nvarchar(max) NOT NULL CONSTRAINT DF_CauHinhs_MauPhu DEFAULT N'#0F766E';",
                "IF COL_LENGTH(N'dbo.CauHinhs', N'MauNen') IS NULL ALTER TABLE dbo.CauHinhs ADD MauNen nvarchar(max) NOT NULL CONSTRAINT DF_CauHinhs_MauNen DEFAULT N'#F7F8FC';",
                "IF COL_LENGTH(N'dbo.CauHinhs', N'MauChu') IS NULL ALTER TABLE dbo.CauHinhs ADD MauChu nvarchar(max) NOT NULL CONSTRAINT DF_CauHinhs_MauChu DEFAULT N'#182235';",
                "IF COL_LENGTH(N'dbo.CauHinhs', N'MauThe') IS NULL ALTER TABLE dbo.CauHinhs ADD MauThe nvarchar(max) NOT NULL CONSTRAINT DF_CauHinhs_MauThe DEFAULT N'#FFFFFF';",
                "IF COL_LENGTH(N'dbo.CauHinhs', N'KieuBoCuc') IS NULL ALTER TABLE dbo.CauHinhs ADD KieuBoCuc nvarchar(max) NOT NULL CONSTRAINT DF_CauHinhs_KieuBoCuc DEFAULT N'rong';",
                "IF COL_LENGTH(N'dbo.CauHinhs', N'KieuChu') IS NULL ALTER TABLE dbo.CauHinhs ADD KieuChu nvarchar(max) NOT NULL CONSTRAINT DF_CauHinhs_KieuChu DEFAULT N'system';",
                "IF COL_LENGTH(N'dbo.CauHinhs', N'CoChu') IS NULL ALTER TABLE dbo.CauHinhs ADD CoChu nvarchar(max) NOT NULL CONSTRAINT DF_CauHinhs_CoChu DEFAULT N'binhthuong';",
                "IF COL_LENGTH(N'dbo.CauHinhs', N'DoRongTrang') IS NULL ALTER TABLE dbo.CauHinhs ADD DoRongTrang int NOT NULL CONSTRAINT DF_CauHinhs_DoRongTrang DEFAULT 1400;",
                "IF COL_LENGTH(N'dbo.CauHinhs', N'SoCotSanPham') IS NULL ALTER TABLE dbo.CauHinhs ADD SoCotSanPham int NOT NULL CONSTRAINT DF_CauHinhs_SoCotSanPham DEFAULT 4;",
                "IF COL_LENGTH(N'dbo.CauHinhs', N'KieuThe') IS NULL ALTER TABLE dbo.CauHinhs ADD KieuThe nvarchar(max) NOT NULL CONSTRAINT DF_CauHinhs_KieuThe DEFAULT N'mem';",
                "IF COL_LENGTH(N'dbo.CauHinhs', N'KieuBong') IS NULL ALTER TABLE dbo.CauHinhs ADD KieuBong nvarchar(max) NOT NULL CONSTRAINT DF_CauHinhs_KieuBong DEFAULT N'nhe';",
                "IF COL_LENGTH(N'dbo.CauHinhs', N'CauTrucTrangChu') IS NULL ALTER TABLE dbo.CauHinhs ADD CauTrucTrangChu nvarchar(max) NOT NULL CONSTRAINT DF_CauHinhs_CauTrucTrangChu DEFAULT N'[\"hero\",\"banner\",\"danhmuc\",\"banchay\",\"moimua\",\"noibat\",\"xephang\",\"ket\"]';",
                "IF COL_LENGTH(N'dbo.CauHinhs', N'CacKhoiAn') IS NULL ALTER TABLE dbo.CauHinhs ADD CacKhoiAn nvarchar(max) NOT NULL CONSTRAINT DF_CauHinhs_CacKhoiAn DEFAULT N'[]';",
                "IF COL_LENGTH(N'dbo.CauHinhs', N'QuaHangNhat') IS NULL ALTER TABLE dbo.CauHinhs ADD QuaHangNhat nvarchar(max) NOT NULL CONSTRAINT DF_CauHinhs_QuaHangNhat DEFAULT N'Voucher mua sắm 500.000đ';",
                "IF COL_LENGTH(N'dbo.CauHinhs', N'QuaHangNhi') IS NULL ALTER TABLE dbo.CauHinhs ADD QuaHangNhi nvarchar(max) NOT NULL CONSTRAINT DF_CauHinhs_QuaHangNhi DEFAULT N'Voucher mua sắm 300.000đ';",
                "IF COL_LENGTH(N'dbo.CauHinhs', N'QuaHangBa') IS NULL ALTER TABLE dbo.CauHinhs ADD QuaHangBa nvarchar(max) NOT NULL CONSTRAINT DF_CauHinhs_QuaHangBa DEFAULT N'Voucher mua sắm 150.000đ';",
                "IF COL_LENGTH(N'dbo.SanPhams', N'CongDungChinh') IS NULL ALTER TABLE dbo.SanPhams ADD CongDungChinh nvarchar(max) NOT NULL CONSTRAINT DF_SanPhams_CongDungChinh DEFAULT N'';",
                "IF COL_LENGTH(N'dbo.SanPhams', N'DoiTuongSuDung') IS NULL ALTER TABLE dbo.SanPhams ADD DoiTuongSuDung nvarchar(max) NOT NULL CONSTRAINT DF_SanPhams_DoiTuongSuDung DEFAULT N'';",
                "IF COL_LENGTH(N'dbo.SanPhams', N'TinhNangNoiBat') IS NULL ALTER TABLE dbo.SanPhams ADD TinhNangNoiBat nvarchar(max) NOT NULL CONSTRAINT DF_SanPhams_TinhNangNoiBat DEFAULT N'';",
                "IF COL_LENGTH(N'dbo.SanPhams', N'LuuYSuDung') IS NULL ALTER TABLE dbo.SanPhams ADD LuuYSuDung nvarchar(max) NOT NULL CONSTRAINT DF_SanPhams_LuuYSuDung DEFAULT N'';",
                "IF COL_LENGTH(N'dbo.NhatKys', N'NguoiDungId') IS NULL ALTER TABLE dbo.NhatKys ADD NguoiDungId int NULL;",
                "IF COL_LENGTH(N'dbo.NhatKys', N'VaiTro') IS NULL ALTER TABLE dbo.NhatKys ADD VaiTro nvarchar(max) NOT NULL CONSTRAINT DF_NhatKys_VaiTro DEFAULT N'';",
                "IF COL_LENGTH(N'dbo.NhatKys', N'Loai') IS NULL ALTER TABLE dbo.NhatKys ADD Loai nvarchar(max) NOT NULL CONSTRAINT DF_NhatKys_Loai DEFAULT N'';",
                "IF COL_LENGTH(N'dbo.NhatKys', N'KetQua') IS NULL ALTER TABLE dbo.NhatKys ADD KetQua nvarchar(max) NOT NULL CONSTRAINT DF_NhatKys_KetQua DEFAULT N'Thành công';",
                "IF COL_LENGTH(N'dbo.NhatKys', N'DiaChiIP') IS NULL ALTER TABLE dbo.NhatKys ADD DiaChiIP nvarchar(max) NOT NULL CONSTRAINT DF_NhatKys_DiaChiIP DEFAULT N'';",
                "IF COL_LENGTH(N'dbo.NhatKys', N'DuongDan') IS NULL ALTER TABLE dbo.NhatKys ADD DuongDan nvarchar(max) NOT NULL CONSTRAINT DF_NhatKys_DuongDan DEFAULT N'';",
                "IF COL_LENGTH(N'dbo.NhatKys', N'TraceId') IS NULL ALTER TABLE dbo.NhatKys ADD TraceId nvarchar(max) NOT NULL CONSTRAINT DF_NhatKys_TraceId DEFAULT N'';",
                "IF OBJECT_ID(N'dbo.NhatKyChiTiets', N'U') IS NULL CREATE TABLE dbo.NhatKyChiTiets (Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_NhatKyChiTiets PRIMARY KEY, ThoiGian datetime2 NOT NULL, NguoiDung nvarchar(max) NOT NULL, NguoiDungId int NULL, VaiTro nvarchar(max) NOT NULL, HanhDong nvarchar(max) NOT NULL, PhuongThuc nvarchar(max) NOT NULL, DuongDan nvarchar(max) NOT NULL, KetQua nvarchar(max) NOT NULL, MaTrangThai int NOT NULL, DiaChiIP nvarchar(max) NOT NULL, TraceId nvarchar(max) NOT NULL, ChiTiet nvarchar(max) NOT NULL);",
                "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_NhatKys_ThoiGian' AND object_id = OBJECT_ID(N'dbo.NhatKys')) CREATE INDEX IX_NhatKys_ThoiGian ON dbo.NhatKys(ThoiGian DESC);",
                "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_NhatKyChiTiets_ThoiGian' AND object_id = OBJECT_ID(N'dbo.NhatKyChiTiets')) CREATE INDEX IX_NhatKyChiTiets_ThoiGian ON dbo.NhatKyChiTiets(ThoiGian DESC);",
                "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_NhatKyChiTiets_NguoiDungId_ThoiGian' AND object_id = OBJECT_ID(N'dbo.NhatKyChiTiets')) CREATE INDEX IX_NhatKyChiTiets_NguoiDungId_ThoiGian ON dbo.NhatKyChiTiets(NguoiDungId, ThoiGian DESC);"
            };

            foreach (var cauLenhSql in cacLenh)
                await coSoDuLieu.Database.ExecuteSqlRawAsync(cauLenhSql);
        }

        // Chuyển các yêu cầu cũ sang bảng nhật ký chi tiết, chỉ thực hiện khi bảng đích chưa có dữ liệu.
        if (await coSoDuLieu.NhatKyChiTiets.CountAsync() == 0 && await coSoDuLieu.NhatKys.AnyAsync())
        {
            var lenhChuyenDuLieu = """
                INSERT INTO NhatKyChiTiets (ThoiGian, NguoiDung, NguoiDungId, VaiTro, HanhDong, PhuongThuc, DuongDan, KetQua, MaTrangThai, DiaChiIP, TraceId, ChiTiet)
                SELECT ThoiGian, NguoiDung, NguoiDungId, VaiTro, HanhDong,
                       CASE WHEN Loai = 'Truy cập' THEN 'GET' ELSE 'POST' END,
                       DuongDan, KetQua,
                       CASE WHEN KetQua = 'Thành công' THEN 200 ELSE 400 END,
                       DiaChiIP, TraceId, ChiTiet
                FROM NhatKys;
                """;
            await coSoDuLieu.Database.ExecuteSqlRawAsync(lenhChuyenDuLieu);
            await coSoDuLieu.Database.ExecuteSqlRawAsync("DELETE FROM NhatKys;");
        }
    }

    /// <summary>Bổ sung tài khoản mẫu và cập nhật quyền cho tài khoản nhân viên.</summary>
    private static async Task BoSungTaiKhoanDemoAsync(AppDb coSoDuLieu)
    {
        var nhanVien = await coSoDuLieu.NguoiDungs.FirstOrDefaultAsync(x => x.Email == "staff@shop.vn");
        if (nhanVien != null) nhanVien.Quyen = "sanpham,donhang,baocao,kho";

        if (!await coSoDuLieu.NguoiDungs.AnyAsync(x => x.Email == "adminphu@shop.vn"))
        {
            coSoDuLieu.NguoiDungs.Add(new NguoiDung
            {
                Email = "adminphu@shop.vn", HoTen = "Quản trị phụ", DienThoai = "0900000003",
                VaiTro = VaiTro.AdminPhu,
                Quyen = "sanpham,danhmuc,kho,donhang,danhgia,lienhe,baocao,banner,baiviet",
                MatKhauHash = MatKhauHelper.Hash("AdminPhu@123")
            });
        }

        // Tạo thêm tài khoản minh họa và dữ liệu mua sắm để tiện kiểm tra các màn hình quản trị.
        var tenDem = new[] { "Minh", "Hoàng", "Tuấn", "Huy", "Đức", "Bảo", "Nam", "Long", "Phúc", "Khang", "Quân", "Duy", "Anh", "Linh", "Trang", "Hương", "Thảo", "Ngọc", "Mai", "Vy" };
        var ho = new[] { "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Vũ", "Đặng", "Bùi", "Đỗ", "Ngô" };
        var tenLot = new[] { "Văn", "Thị", "Minh", "Gia", "Quốc", "Thanh", "Xuân", "Hữu", "Đức", "Khánh" };
        for (var i = 1; i <= 120; i++)
        {
            var email = $"khach{i:000}@example.com";
            if (await coSoDuLieu.NguoiDungs.AnyAsync(x => x.Email == email)) continue;
            coSoDuLieu.NguoiDungs.Add(new NguoiDung
            {
                Email = email,
                HoTen = $"{ho[i % ho.Length]} {tenLot[(i / ho.Length + i % tenLot.Length) % tenLot.Length]} {tenDem[(i * 7 + i / tenDem.Length) % tenDem.Length]}",
                DienThoai = $"09{(20000000 + i * 1379):00000000}",
                VaiTro = VaiTro.KhachHang,
                MatKhauHash = MatKhauHelper.Hash("Khach@123"),
                NgayTao = DateTime.Now.AddDays(-((i * 13) % 340))
            });
        }
        await coSoDuLieu.SaveChangesAsync();
    }

    /// <summary>Tạo đơn hàng, đánh giá và sản phẩm yêu thích mẫu khi cơ sở dữ liệu chưa có đơn hàng.</summary>
    private static async Task TaoDuLieuMuaSamDemoAsync(AppDb coSoDuLieu)
    {
        // Chỉ sinh một lần khi database chưa có đơn hàng; không đụng vào tồn kho hiện tại.
        if (await coSoDuLieu.DonHangs.AnyAsync()) return;
        var sanPhams = await coSoDuLieu.SanPhams.Where(x => x.DangBan).OrderBy(x => x.Id).ToListAsync();
        var khachHangs = await coSoDuLieu.NguoiDungs.Where(x => x.VaiTro == VaiTro.KhachHang && !x.BiKhoa).OrderBy(x => x.Id).ToListAsync();
        if (sanPhams.Count == 0 || khachHangs.Count == 0) return;

        var ngauNhien = new Random(20261112);
        var quanHuyens = new[] { "Cầu Giấy", "Thanh Xuân", "Đống Đa", "Hà Đông", "Hoàng Mai", "Nam Từ Liêm", "Long Biên", "Hai Bà Trưng", "Ba Đình", "Bắc Từ Liêm" };
        var phuongXas = new[] { "Phường Dịch Vọng", "Phường Khương Trung", "Phường Trung Hòa", "Phường Mộ Lao", "Phường Mai Dịch", "Phường Vĩnh Tuy" };
        var donHangs = new List<DonHang>();
        var luotMuaHoanTat = new HashSet<(int NguoiDungId, int SanPhamId)>();
        var soLuongBanTheoSanPham = new Dictionary<int, int>();

        // Đơn hàng kiểm thử từ khách demo dùng để xác minh 3 đánh giá mẫu có giao dịch hợp lệ.
        var khachMau = khachHangs.FirstOrDefault(x => x.Email == "khach@shop.vn") ?? khachHangs[0];
        var donHangMau = new DonHang
        {
            MaDon = "DHDEMO-001", NguoiDungId = khachMau.Id, HoTen = khachMau.HoTen,
            DienThoai = khachMau.DienThoai, DiaChi = "Số 18 Nguyễn Trãi, Thanh Xuân, Hà Nội",
            GhiChu = "Đơn hàng minh họa để kiểm tra đánh giá đã mua", TrangThai = TrangThaiDon.HoanThanh,
            PhuongThucThanhToan = PhuongThucThanhToan.COD, ThanhToan = ThanhToan.DaThanhToan,
            NgayTao = DateTime.Today.AddDays(-8)
        };
        foreach (var sp in sanPhams.Take(3))
        {
            donHangMau.ChiTiet.Add(new ChiTietDonHang { SanPhamId = sp.Id, TenSanPham = sp.Ten, AnhSanPham = sp.AnhChinh, DonGia = sp.GiaHienTai, SoLuong = 1 });
            donHangMau.TongTien += sp.GiaHienTai;
            luotMuaHoanTat.Add((khachMau.Id, sp.Id));
            soLuongBanTheoSanPham[sp.Id] = soLuongBanTheoSanPham.GetValueOrDefault(sp.Id) + 1;
        }
        donHangs.Add(donHangMau);

        for (var i = 1; i <= 260; i++)
        {
            var khachHang = khachHangs[ngauNhien.Next(khachHangs.Count)];
            var xacSuat = ngauNhien.Next(100);
            var trangThai = xacSuat < 70 ? TrangThaiDon.HoanThanh
                : xacSuat < 79 ? TrangThaiDon.DangGiao
                : xacSuat < 88 ? TrangThaiDon.DaXacNhan
                : xacSuat < 95 ? TrangThaiDon.ChoXacNhan : TrangThaiDon.DaHuy;
            var soNgayTruoc = ngauNhien.Next(100) < 30 ? ngauNhien.Next(0, 30)
                : ngauNhien.Next(100) < 55 ? ngauNhien.Next(30, 120) : ngauNhien.Next(120, 365);
            var ngayTao = DateTime.Now.AddDays(-soNgayTruoc).AddHours(-ngauNhien.Next(0, 12));
            var phuongThucTT = ngauNhien.Next(2) == 0 ? PhuongThucThanhToan.COD : PhuongThucThanhToan.ChuyenKhoan;
            var daThanhToan = trangThai == TrangThaiDon.HoanThanh || (trangThai != TrangThaiDon.DaHuy && phuongThucTT == PhuongThucThanhToan.ChuyenKhoan && ngauNhien.Next(100) < 38);
            var donHang = new DonHang
            {
                MaDon = $"DH{ngayTao:yyMMdd}{i:0000}", NguoiDungId = khachHang.Id, HoTen = khachHang.HoTen,
                DienThoai = khachHang.DienThoai,
                DiaChi = $"Số {ngauNhien.Next(1, 250)}, {phuongXas[ngauNhien.Next(phuongXas.Length)]}, {quanHuyens[ngauNhien.Next(quanHuyens.Length)]}, Hà Nội",
                GhiChu = ngauNhien.Next(4) == 0 ? "Giao giờ hành chính, gọi trước khi đến" : "Không có",
                TrangThai = trangThai, PhuongThucThanhToan = phuongThucTT,
                ThanhToan = daThanhToan ? ThanhToan.DaThanhToan : ThanhToan.ChuaThanhToan,
                NgayTao = ngayTao
            };
            var soMatHang = ngauNhien.Next(1, 4);
            var sanPhamDuocChon = sanPhams.OrderBy(_ => ngauNhien.Next()).Take(soMatHang).ToList();
            foreach (var sp in sanPhamDuocChon)
            {
                var soLuong = ngauNhien.Next(1, 3);
                var donGia = sp.GiaHienTai;
                donHang.ChiTiet.Add(new ChiTietDonHang { SanPhamId = sp.Id, TenSanPham = sp.Ten, AnhSanPham = sp.AnhChinh, DonGia = donGia, SoLuong = soLuong });
                donHang.TongTien += donGia * soLuong;
                if (trangThai != TrangThaiDon.DaHuy)
                {
                    soLuongBanTheoSanPham[sp.Id] = soLuongBanTheoSanPham.GetValueOrDefault(sp.Id) + soLuong;
                    if (trangThai == TrangThaiDon.HoanThanh && daThanhToan) luotMuaHoanTat.Add((khachHang.Id, sp.Id));
                }
            }
            donHangs.Add(donHang);
        }
        coSoDuLieu.DonHangs.AddRange(donHangs);
        foreach (var sp in sanPhams) sp.DaBan = soLuongBanTheoSanPham.GetValueOrDefault(sp.Id);
        await coSoDuLieu.SaveChangesAsync();

        var danhGiaDaCo = await coSoDuLieu.DanhGias.Select(x => new { x.NguoiDungId, x.SanPhamId }).ToListAsync();
        var khoaDanhGia = danhGiaDaCo.Select(x => (x.NguoiDungId, x.SanPhamId)).ToHashSet();
        var tieuDeDanhGia = new[] { "Dùng rất ổn", "Đáng mua trong tầm giá", "Đóng gói cẩn thận", "Chất lượng tốt", "Hài lòng", "Sẽ ủng hộ tiếp", "Đúng mô tả" };
        var noiDungDanhGia = new[]
        {
            "Sản phẩm dùng ổn định, hoàn thiện tốt và dễ sử dụng.",
            "Shop đóng gói cẩn thận, giao hàng đúng hẹn. Mình khá hài lòng.",
            "Chất lượng đúng như mô tả, phù hợp nhu cầu gia đình.",
            "Giá hợp lý, thao tác đơn giản. Dùng một thời gian vẫn hoạt động tốt.",
            "Nhân viên hỗ trợ nhanh, sản phẩm không có lỗi và hoạt động tốt."
        };
        var danhGias = new List<DanhGia>();
        foreach (var luotMua in luotMuaHoanTat.OrderBy(_ => ngauNhien.Next()).Take(90))
        {
            if (!khoaDanhGia.Add(luotMua)) continue;
            danhGias.Add(new DanhGia
            {
                NguoiDungId = luotMua.NguoiDungId, SanPhamId = luotMua.SanPhamId,
                SoSao = ngauNhien.Next(3, 6), TieuDe = tieuDeDanhGia[ngauNhien.Next(tieuDeDanhGia.Length)],
                NoiDung = noiDungDanhGia[ngauNhien.Next(noiDungDanhGia.Length)], DaDuyet = ngauNhien.Next(100) < 82,
                NgayTao = DateTime.Now.AddDays(-ngauNhien.Next(1, 180))
            });
        }
        if (danhGias.Count > 0)
        {
            coSoDuLieu.DanhGias.AddRange(danhGias);
            await coSoDuLieu.SaveChangesAsync();
        }

        var yeuThichs = new List<YeuThich>();
        foreach (var khachHang in khachHangs.Take(70))
        {
            foreach (var sp in sanPhams.OrderBy(_ => ngauNhien.Next()).Take(ngauNhien.Next(1, 4)))
            {
                if (!await coSoDuLieu.YeuThichs.AnyAsync(x => x.NguoiDungId == khachHang.Id && x.SanPhamId == sp.Id))
                    yeuThichs.Add(new YeuThich { NguoiDungId = khachHang.Id, SanPhamId = sp.Id });
            }
        }
        if (yeuThichs.Count > 0) { coSoDuLieu.YeuThichs.AddRange(yeuThichs); await coSoDuLieu.SaveChangesAsync(); }
    }
}
