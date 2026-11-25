using System.Text.Json;
using GIADUNGCHATLUONG.DuLieu;
using GIADUNGCHATLUONG.MoHinh;

namespace GIADUNGCHATLUONG.TienIch;

/// <summary>Đọc, lưu và cập nhật giỏ hàng của người dùng trong Session.</summary>
public static class GioHangHelper
{
    private const string KHOA = "gio_hang";

    public static Dictionary<int, int> Lay(ISession session)
    {
        var json = session.GetString(KHOA);
        return string.IsNullOrEmpty(json)
            ? new Dictionary<int, int>()
            : JsonSerializer.Deserialize<Dictionary<int, int>>(json) ?? new();
    }

    public static void Luu(ISession session, Dictionary<int, int> gio) =>
        session.SetString(KHOA, JsonSerializer.Serialize(gio));

    public static int TongSoLuong(ISession session) => Lay(session).Values.Sum();

    public static List<DongGioHang> DanhSach(ISession session, AppDb db)
    {
        var gio = Lay(session);
        if (gio.Count == 0) return new();
        var ids = gio.Keys.ToList();
        var sps = db.SanPhams.Where(p => ids.Contains(p.Id) && p.DangBan).ToList();
        var idsConKinhDoanh = sps.Select(p => p.Id).ToHashSet();
        var daThayDoi = false;
        foreach (var id in gio.Keys.Where(id => !idsConKinhDoanh.Contains(id)).ToList())
        {
            gio.Remove(id);
            daThayDoi = true;
        }
        if (daThayDoi) Luu(session, gio);

        var list = new List<DongGioHang>();
        foreach (var sp in sps)
        {
            var soLuong = Math.Max(1, gio.GetValueOrDefault(sp.Id));
            list.Add(new DongGioHang { SanPham = sp, SoLuong = soLuong });
        }
        return list;
    }

    public static void Them(ISession session, int sanPhamId, int soLuong = 1)
    {
        var gio = Lay(session);
        gio[sanPhamId] = gio.GetValueOrDefault(sanPhamId) + soLuong;
        Luu(session, gio);
    }

    public static void Dat(ISession session, int sanPhamId, int soLuong)
    {
        var gio = Lay(session);
        if (soLuong <= 0) gio.Remove(sanPhamId);
        else gio[sanPhamId] = soLuong;
        Luu(session, gio);
    }

    public static void Xoa(ISession session, int sanPhamId)
    {
        var gio = Lay(session);
        gio.Remove(sanPhamId);
        Luu(session, gio);
    }

    public static void XoaHet(ISession session) => Luu(session, new());
}
