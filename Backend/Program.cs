using System.Security.Claims;
using GIADUNGCHATLUONG.DuLieu;
using GIADUNGCHATLUONG.TienIch;
using GIADUNGCHATLUONG.MoHinh;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var boTaoUngDung = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = Path.Combine("Frontend", "TaiNguyen")
});

// Tạo thư mục Database nếu chưa có. Tệp SQLite sẽ được lưu tại đây.
Directory.CreateDirectory(Path.Combine(boTaoUngDung.Environment.ContentRootPath, "Database"));

// Đăng ký MVC và chỉ rõ nơi chứa các trang giao diện Razor (.cshtml).
boTaoUngDung.Services.AddControllersWithViews().AddRazorOptions(options =>
{
    options.ViewLocationFormats.Clear();
    options.ViewLocationFormats.Add("/Frontend/GiaoDien/{1}/{0}.cshtml");
    options.ViewLocationFormats.Add("/Frontend/GiaoDien/Chung/{0}.cshtml");
});
// Chọn loại cơ sở dữ liệu trong appsettings.json. Hiện dự án đang cấu hình SQLite.
var nhaCungCap = boTaoUngDung.Configuration["Database:Provider"] ?? "SqlServer";
// Đăng ký lớp AppDb: đây là cầu nối giữa mã C# và các bảng trong Database.
boTaoUngDung.Services.AddDbContext<AppDb>(options =>
{
    if (nhaCungCap.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        var chuoiKetNoi = boTaoUngDung.Configuration.GetConnectionString("SQLiteConnection")
            ?? "Data Source=Database/cua-hang.db;Foreign Keys=True";
        options.UseSqlite(chuoiKetNoi);
    }
    else if (nhaCungCap.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        var chuoiKetNoi = boTaoUngDung.Configuration.GetConnectionString("SqlServerConnection")
            ?? throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:SqlServerConnection.");
        options.UseSqlServer(chuoiKetNoi);
    }
    else
    {
        throw new InvalidOperationException($"Nhà cung cấp cơ sở dữ liệu '{nhaCungCap}' chưa được hỗ trợ.");
    }
});

// Cấu hình đăng nhập bằng Cookie. Các trang cần đăng nhập sẽ kiểm tra Cookie này.
boTaoUngDung.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "GIADUNGCHATLUONG.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/tai-khoan/dang-nhap";
        options.AccessDeniedPath = "/tai-khoan/tu-choi";
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
        options.Events = new CookieAuthenticationEvents
        {
            // Kiểm tra lại trạng thái khóa và quyền để cookie đăng nhập cũ không giữ quyền đã bị thu hồi.
            OnValidatePrincipal = async boiCanh =>
            {
                var danhTinh = boiCanh.Principal;
                var giaTriId = danhTinh?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(giaTriId, out var maNguoiDung))
                {
                    boiCanh.RejectPrincipal();
                    await boiCanh.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                var coSoDuLieu = boiCanh.HttpContext.RequestServices.GetRequiredService<AppDb>();
                var taiKhoan = await coSoDuLieu.NguoiDungs.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == maNguoiDung);
                if (taiKhoan == null || taiKhoan.BiKhoa)
                {
                    boiCanh.RejectPrincipal();
                    await boiCanh.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                var cacQuyen = QuyenHelper.HieuLuc(taiKhoan.VaiTro, taiKhoan.Quyen);
                var quyenCu = danhTinh!.FindAll("Quyen").Select(x => x.Value).OrderBy(x => x).ToArray();
                var quyenMoi = cacQuyen.OrderBy(x => x).ToArray();
                var canCapNhatCookie = danhTinh.FindFirstValue(ClaimTypes.Role) != taiKhoan.VaiTro
                    || danhTinh.FindFirstValue(ClaimTypes.Name) != taiKhoan.HoTen
                    || danhTinh.FindFirstValue(ClaimTypes.Email) != taiKhoan.Email
                    || !quyenCu.SequenceEqual(quyenMoi);

                if (canCapNhatCookie)
                {
                    var cacThongTinDangNhap = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, taiKhoan.Id.ToString()),
                        new(ClaimTypes.Name, taiKhoan.HoTen),
                        new(ClaimTypes.Email, taiKhoan.Email),
                        new(ClaimTypes.Role, taiKhoan.VaiTro)
                    };
                    cacThongTinDangNhap.AddRange(cacQuyen.Select(quyen => new Claim("Quyen", quyen)));
                    var danhTinhMoi = new ClaimsIdentity(cacThongTinDangNhap, CookieAuthenticationDefaults.AuthenticationScheme);
                    boiCanh.ReplacePrincipal(new ClaimsPrincipal(danhTinhMoi));
                    boiCanh.ShouldRenew = true;
                }
            }
        };
    });

boTaoUngDung.Services.AddAuthorization();
boTaoUngDung.Services.AddDistributedMemoryCache();
boTaoUngDung.Services.AddSession(options =>
{
    options.Cookie.Name = "GIADUNGCHATLUONG.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.IdleTimeout = TimeSpan.FromHours(4);
});

// Hoàn tất cấu hình và tạo ứng dụng web.
var ungDung = boTaoUngDung.Build();

// Tạo bảng cần thiết và dữ liệu mẫu khi website khởi động.
using (var phamVi = ungDung.Services.CreateScope())
{
    var coSoDuLieu = phamVi.ServiceProvider.GetRequiredService<AppDb>();
    await KhoiTaoDuLieu.KhoiTaoAsync(coSoDuLieu);
}

if (!ungDung.Environment.IsDevelopment())
{
    // Hiển thị trang lỗi chung, tránh phát sinh vòng lặp khi chính trang lỗi cũng không truy cập được.
    ungDung.UseExceptionHandler(errorApp => errorApp.Run(async boiCanh =>
    {
        boiCanh.Response.StatusCode = StatusCodes.Status500InternalServerError;
        boiCanh.Response.ContentType = "text/html; charset=utf-8";
        await boiCanh.Response.WriteAsync("<!doctype html><html lang=\"vi\"><meta charset=\"utf-8\"><title>Lỗi hệ thống</title><body style=\"font-family:Arial,sans-serif;padding:40px;line-height:1.6\"><h1>Rất tiếc, đã có lỗi xảy ra.</h1><p>Vui lòng tải lại trang hoặc quay lại sau.</p><a href=\"/\">Về trang chủ</a></body></html>");
    }));
    ungDung.UseHsts();
}

// Các bước xử lý yêu cầu: tệp tĩnh -> định tuyến -> Session -> đăng nhập -> phân quyền.
ungDung.UseHttpsRedirection();
ungDung.UseStaticFiles();
ungDung.UseRouting();
ungDung.UseSession();
ungDung.UseAuthentication();
// Đặt nhật ký sau xác thực để ghi đúng tài khoản, vai trò và cả những lần bị từ chối quyền.
ungDung.UseMiddleware<NhatKyMiddleware>();
ungDung.UseAuthorization();

// Địa chỉ mặc định sẽ mở TrangChuController và hàm Index.
ungDung.MapControllerRoute("default", "{controller=TrangChu}/{action=Index}/{id?}");
ungDung.Run();
