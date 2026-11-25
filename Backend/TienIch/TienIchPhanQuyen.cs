using System.Security.Claims;
using GIADUNGCHATLUONG.MoHinh;

namespace GIADUNGCHATLUONG.TienIch;

/// <summary>Kiểm tra vai trò và quyền truy cập các chức năng của website.</summary>
public static class QuyenHelper
{
    public record QuyenItem(string Ma, string Ten, string Nhom);

    public static readonly QuyenItem[] TatCa =
    {
        new("sanpham", "Quản lý sản phẩm", "Cửa hàng"),
        new("danhmuc", "Quản lý danh mục", "Cửa hàng"),
        new("kho", "Quản lý kho hàng", "Cửa hàng"),
        new("donhang", "Quản lý đơn hàng", "Bán hàng"),
        new("danhgia", "Duyệt đánh giá", "Bán hàng"),
        new("lienhe", "Quản lý liên hệ", "Bán hàng"),
        new("baocao", "Xem doanh thu & báo cáo", "Báo cáo"),
        new("banner", "Quản lý banner", "Nội dung"),
        new("baiviet", "Quản lý bài viết", "Nội dung")
    };

    public static readonly string[] MacDinh = { "sanpham", "donhang", "kho" };

    public static string[] HieuLuc(string vaiTro, string quyen)
    {
        if (vaiTro == VaiTro.Admin) return TatCa.Select(x => x.Ma).ToArray();
        if (vaiTro is not (VaiTro.NhanVien or VaiTro.AdminPhu)) return Array.Empty<string>();
        var s = (quyen ?? "").Trim();
        if (s == "") return MacDinh;
        if (s == "-") return Array.Empty<string>();
        return s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => TatCa.Any(q => q.Ma == x)).Distinct().ToArray();
    }

    public static string Luu(IEnumerable<string> maDaChon)
    {
        var daChon = (maDaChon ?? Array.Empty<string>()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var list = TatCa.Where(x => daChon.Contains(x.Ma)).Select(x => x.Ma).ToArray();
        return list.Length == 0 ? "-" : string.Join(",", list);
    }

    public static bool CoQuyen(ClaimsPrincipal user, string ma)
    {
        if (user.IsInRole(VaiTro.Admin)) return true;
        return user.HasClaim("Quyen", ma);
    }
}
