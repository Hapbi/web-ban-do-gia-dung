using System.ComponentModel.DataAnnotations;

namespace GIADUNGCHATLUONG.MoHinh;

/// <summary>
/// Dữ liệu người mua nhập ở bước đặt hàng. Giá và sản phẩm luôn được lấy lại từ database,
/// không nhận các giá trị tổng tiền do trình duyệt gửi lên.
/// </summary>
public class ThanhToanInput
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(120, ErrorMessage = "Họ tên không được vượt quá 120 ký tự.")]
    public string HoTen { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [StringLength(20, ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string DienThoai { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng.")]
    [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự.")]
    public string DiaChi { get; set; } = "";

    [Required(ErrorMessage = "Ghi chú là bắt buộc. Anh/chị vui lòng nhập ghi chú giao hàng hoặc nhập 'Không có'.")]
    [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự.")]
    public string GhiChu { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
    [RegularExpression("^(COD|ChuyenKhoan)$", ErrorMessage = "Phương thức thanh toán không hợp lệ.")]
    public string PhuongThuc { get; set; } = PhuongThucThanhToan.COD;
}
