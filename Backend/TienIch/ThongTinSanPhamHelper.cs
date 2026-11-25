using System.Globalization;
using System.Text;
using GIADUNGCHATLUONG.MoHinh;

namespace GIADUNGCHATLUONG.TienIch;

/// <summary>Chuẩn hóa và hiển thị thông số kỹ thuật của sản phẩm.</summary>
/// <summary>Một dòng thông số đã được tách thành tên, giá trị và biểu tượng.</summary>
public sealed record ThongSoHienThi(string Ten, string GiaTri, string BieuTuong);

/// <summary>
/// Gom các quy tắc hiển thị sản phẩm vào một nơi để trang chi tiết và trang quản trị dùng chung.
/// Mỗi sản phẩm có thể có những trường thông số riêng phù hợp với nó.
/// </summary>
public static class ThongTinSanPhamHelper
{
    /// <summary>Tách nội dung nhiều dòng thành danh sách thông số.</summary>
    public static List<ThongSoHienThi> TachThongSo(string? noiDung)
    {
        var ketQua = new List<ThongSoHienThi>();
        if (string.IsNullOrWhiteSpace(noiDung)) return ketQua;

        foreach (var dong in noiDung.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var viTriHaiCham = dong.IndexOfAny(new[] { ':', '：' });
            var ten = viTriHaiCham > 0 ? dong[..viTriHaiCham].Trim() : "Thông tin";
            var giaTri = viTriHaiCham > 0 ? dong[(viTriHaiCham + 1)..].Trim() : dong.Trim();
            if (giaTri.Length == 0) continue;
            ketQua.Add(new ThongSoHienThi(ten, giaTri, LayBieuTuong(ten)));
        }

        return ketQua;
    }

    /// <summary>Gợi ý các thông số thường gặp theo danh mục sản phẩm đang chọn.</summary>
    public static IReadOnlyList<string> LayTruongThongSoGoiY(string? tenDanhMuc)
    {
        var danhMuc = ChuanHoa(tenDanhMuc);
        if (danhMuc.Contains("bep"))
            return new[] { "Dung tích", "Công suất", "Chất liệu", "Kích thước", "Chế độ hoạt động", "Tính năng an toàn", "Phụ kiện đi kèm", "Bảo hành" };
        if (danhMuc.Contains("dien gia dung"))
            return new[] { "Công suất", "Điện áp", "Số chế độ", "Mức độ ồn", "Diện tích sử dụng", "Kích thước", "Chất liệu", "Bảo hành" };
        if (danhMuc.Contains("ve sinh"))
            return new[] { "Lực hút / Công suất", "Dung lượng pin", "Thời gian sử dụng", "Diện tích làm sạch", "Dung tích hộp chứa", "Bộ lọc", "Chức năng", "Bảo hành" };
        if (danhMuc.Contains("cham soc"))
            return new[] { "Công suất", "Mức nhiệt", "Chế độ gió", "Chất liệu", "Chiều dài dây", "Khối lượng", "Tính năng an toàn", "Bảo hành" };
        if (danhMuc.Contains("chieu sang"))
            return new[] { "Công suất", "Loại bóng", "Màu ánh sáng", "Mức sáng", "Nguồn điện", "Kích thước", "Chất liệu", "Bảo hành" };
        if (danhMuc.Contains("dien cam tay") || danhMuc.Contains("dung cu"))
            return new[] { "Công suất", "Điện áp", "Tốc độ", "Lực mô-men xoắn", "Chất liệu", "Phụ kiện đi kèm", "Khối lượng", "Bảo hành" };
        if (danhMuc.Contains("phong ngu"))
            return new[] { "Kích thước", "Chất liệu", "Màu sắc", "Khối lượng", "Công dụng", "Cách vệ sinh", "Bảo hành" };

        return new[] { "Kích thước", "Khối lượng", "Chất liệu", "Màu sắc", "Số lượng", "Công dụng", "Xuất xứ", "Bảo hành" };
    }

    /// <summary>Trả về nhóm người dùng phù hợp; cửa hàng có thể sửa riêng cho từng sản phẩm.</summary>
    public static string LayDoiTuongMacDinh(SanPham sanPham)
    {
        if (!string.IsNullOrWhiteSpace(sanPham.DoiTuongSuDung)) return sanPham.DoiTuongSuDung;

        // Ưu tiên nhận diện theo tên hàng để đối tượng phù hợp sát với từng mặt hàng hơn.
        var ten = ChuanHoa(sanPham.Ten);
        if (ten.Contains("noi com") || ten.Contains("am sieu toc") || ten.Contains("may xay")
            || ten.Contains("chao") || ten.Contains("noi chien"))
            return "Gia đình thường xuyên chuẩn bị bữa ăn và cần thiết bị hỗ trợ công việc nhà bếp.";
        if (ten.Contains("may say toc")) return "Người cần làm khô tóc tại nhà và muốn chủ động chăm sóc cá nhân.";
        if (ten.Contains("den ban")) return "Học sinh, sinh viên hoặc người cần ánh sáng tập trung tại bàn học và bàn làm việc.";
        if (ten.Contains("robot hut bui")) return "Gia đình muốn hỗ trợ việc hút bụi, lau sàn và giảm thao tác vệ sinh hằng ngày.";
        if (ten.Contains("hut bui")) return "Gia đình cần vệ sinh sàn, khe nhỏ hoặc các bề mặt phù hợp với đầu hút.";
        if (ten.Contains("may loc khong khi")) return "Người muốn lọc không khí trong phòng có diện tích phù hợp với thông số máy.";
        if (ten.Contains("quat")) return "Người cần lưu thông không khí trong phòng ngủ, phòng khách hoặc nơi làm việc.";
        if (ten.Contains("may khoan") || ten.Contains("tua vit")) return "Người thường sửa chữa, lắp đặt hoặc làm các công việc DIY trong gia đình.";
        if (ten.Contains("hop dung thuc pham")) return "Gia đình cần chia phần, bảo quản và sắp xếp thực phẩm gọn gàng.";

        var danhMuc = ChuanHoa(sanPham.DanhMuc?.Ten);
        if (danhMuc.Contains("bep")) return "Gia đình thường xuyên nấu ăn và cần thiết bị hỗ trợ việc bếp núc.";
        if (danhMuc.Contains("dien gia dung")) return "Gia đình muốn tiết kiệm thời gian và tăng tiện nghi sinh hoạt.";
        if (danhMuc.Contains("ve sinh")) return "Gia đình cần giữ nhà cửa sạch sẽ, đặc biệt ở khu vực thường xuyên sử dụng.";
        if (danhMuc.Contains("cham soc")) return "Người cần thiết bị chăm sóc cá nhân tại nhà.";
        if (danhMuc.Contains("chieu sang")) return "Học sinh, sinh viên hoặc người cần thêm ánh sáng khi học tập và làm việc.";
        if (danhMuc.Contains("dien cam tay") || danhMuc.Contains("dung cu")) return "Người dùng gia đình hoặc người thực hiện công việc sửa chữa, lắp đặt.";
        if (danhMuc.Contains("phong ngu")) return "Gia đình muốn bổ sung tiện ích cho không gian nghỉ ngơi.";
        return "Gia đình cần một vật dụng tiện ích cho sinh hoạt hằng ngày.";
    }

    /// <summary>Ưu tiên công dụng do cửa hàng nhập; dùng nội dung phù hợp với tên hàng cho dữ liệu cũ.</summary>
    public static string LayCongDungMacDinh(SanPham sanPham)
    {
        if (!string.IsNullOrWhiteSpace(sanPham.CongDungChinh)) return sanPham.CongDungChinh;
        var ten = ChuanHoa(sanPham.Ten);

        if (ten.Contains("noi com")) return "Nấu cơm và giữ ấm cơm sau khi nấu.";
        if (ten.Contains("am sieu toc")) return "Đun nước nhanh để pha trà, cà phê và chuẩn bị đồ uống.";
        if (ten.Contains("may xay sinh to")) return "Xay trái cây, rau củ và nguyên liệu theo hướng dẫn của nhà sản xuất.";
        if (ten.Contains("chao")) return "Hỗ trợ chiên, xào và áp chảo thực phẩm.";
        if (ten.Contains("noi chien")) return "Làm chín thực phẩm bằng luồng khí nóng.";
        if (ten.Contains("quat")) return "Tạo luồng gió lưu thông không khí trong phòng.";
        if (ten.Contains("may loc khong khi")) return "Lọc không khí trong không gian sử dụng theo thông số của bộ lọc.";
        if (ten.Contains("ban ui")) return "Làm phẳng quần áo bằng nhiệt hoặc hơi nước.";
        if (ten.Contains("robot hut bui")) return "Hỗ trợ hút bụi và lau sàn tự động.";
        if (ten.Contains("hut bui")) return "Hút bụi trên sàn, bề mặt hoặc khu vực phù hợp với đầu hút.";
        if (ten.Contains("cay lau")) return "Hỗ trợ lau sàn và vắt nước trong quá trình vệ sinh.";
        if (ten.Contains("den ban")) return "Bổ sung ánh sáng cho bàn học hoặc bàn làm việc.";
        if (ten.Contains("may say toc")) return "Sấy khô tóc bằng luồng khí nóng hoặc mát.";
        if (ten.Contains("may khoan")) return "Khoan lỗ trên vật liệu phù hợp với mũi khoan và hướng dẫn sử dụng.";
        if (ten.Contains("tua vit")) return "Siết và tháo các loại vít tương thích.";
        if (ten.Contains("hop dung thuc pham")) return "Bảo quản và sắp xếp thực phẩm trong gia đình.";

        return string.IsNullOrWhiteSpace(sanPham.MoTa)
            ? "Hỗ trợ một nhu cầu sinh hoạt trong gia đình. Vui lòng xem thông số để chọn đúng sản phẩm."
            : sanPham.MoTa;
    }

    /// <summary>Chọn biểu tượng nhỏ dựa trên tên thông số.</summary>
    public static string LayBieuTuong(string tenThongSo)
    {
        var ten = ChuanHoa(tenThongSo);
        if (ten.Contains("cong suat")) return "⚡";
        if (ten.Contains("dung tich")) return "💧";
        if (ten.Contains("kich thuoc") || ten.Contains("duong kinh")) return "📐";
        if (ten.Contains("khoi luong") || ten.Contains("trong luong")) return "⚖️";
        if (ten.Contains("pin")) return "🔋";
        if (ten.Contains("bao hanh")) return "🛡️";
        if (ten.Contains("chat lieu")) return "🧱";
        if (ten.Contains("dien ap") || ten.Contains("nguon dien")) return "🔌";
        if (ten.Contains("dien tich")) return "📏";
        if (ten.Contains("toc do") || ten.Contains("che do")) return "🎛️";
        if (ten.Contains("mau sac") || ten.Contains("anh sang")) return "🎨";
        if (ten.Contains("so luong") || ten.Contains("so mon")) return "📦";
        if (ten.Contains("do on")) return "🔈";
        if (ten.Contains("xuat xu")) return "🌍";
        return "🔹";
    }

    /// <summary>Bỏ dấu tiếng Việt để so khớp danh mục và tên hàng ổn định hơn.</summary>
    private static string ChuanHoa(string? giaTri)
    {
        if (string.IsNullOrWhiteSpace(giaTri)) return "";
        var chuoi = giaTri.Normalize(NormalizationForm.FormD);
        var ketQua = new StringBuilder(chuoi.Length);
        foreach (var kyTu in chuoi)
        {
            var loai = CharUnicodeInfo.GetUnicodeCategory(kyTu);
            if (loai != UnicodeCategory.NonSpacingMark) ketQua.Append(kyTu);
        }
        return ketQua.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant().Replace('đ', 'd');
    }
}
