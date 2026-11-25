using System.Globalization;
using System.Security.Claims;

namespace GIADUNGCHATLUONG.TienIch;

/// <summary>Các hàm dùng chung trong nhiều phần của website.</summary>
public static class TienIch
{
    public static string Tien(decimal n) =>
        n.ToString("#,##0", CultureInfo.InvariantCulture).Replace(',', '.') + "đ";
    public static string Ngay(DateTime d) => d.ToString("dd/MM/yyyy");
    public static string NgayGio(DateTime d) => d.ToString("dd/MM/yyyy HH:mm");

    public static int? LayIdNguoiDung(ClaimsPrincipal user)
    {
        if (user?.Identity?.IsAuthenticated != true) return null;
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var v) ? v : null;
    }

    public static bool LaQuanTri(ClaimsPrincipal user) =>
        QuyenHelper.CoQuyen(user, "donhang");
}
