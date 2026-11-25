# 🏠 GIADUNGCHATLUONG – Website bán đồ gia dụng

Website bán đồ gia dụng xây dựng bằng **ASP.NET Core MVC (.NET 8)**, gồm phần cho **khách hàng** mua sắm và phần cho **quản trị viên** quản lý cửa hàng.

## ✨ Chức năng chính

### 🛍️ Khách hàng
- Xem trang chủ: banner, danh mục, sản phẩm bán chạy / mới
- Xem danh sách sản phẩm: tìm kiếm, lọc theo danh mục và giá, sắp xếp
- Xem chi tiết sản phẩm
- Thêm vào giỏ hàng, sửa số lượng, xóa sản phẩm
- Mua ngay
- Đặt hàng, thanh toán COD hoặc chuyển khoản
- Xem lịch sử và trạng thái đơn hàng
- Yêu thích sản phẩm, đánh giá sản phẩm đã mua
- Đăng ký, đăng nhập, đổi mật khẩu
- Gửi liên hệ cho cửa hàng
- Bảng xếp hạng khách hàng mua nhiều nhất

### 🛠️ Quản trị viên
- Thống kê tổng quan: doanh thu, số đơn, cảnh báo hàng sắp hết
- Quản lý sản phẩm, danh mục, kho hàng
- Quản lý đơn hàng và cập nhật trạng thái
- Duyệt đánh giá, xem liên hệ của khách
- Quản lý banner và bài viết
- Báo cáo doanh thu, xuất file CSV
- Quản lý người dùng và phân quyền (Admin / Nhân viên / Khách hàng)
- Cấu hình thông tin cửa hàng, đổi màu giao diện
- Xem nhật ký hoạt động

## 🧰 Công nghệ
| Phần | Công nghệ |
|---|---|
| Backend | C#, ASP.NET Core MVC (.NET 8) |
| Frontend | Razor, HTML, CSS, JavaScript |
| Cơ sở dữ liệu | SQLite (Entity Framework Core) |

## 📂 Cấu trúc thư mục
```text
GIADUNGCHATLUONG/
├── Frontend/   # Giao diện (trang Razor, CSS, JS)
├── Backend/    # Xử lý (Controller, Model)
└── Database/   # Cơ sở dữ liệu SQLite
```

## 👤 Tài khoản demo
| Vai trò | Email | Mật khẩu |
|---|---|---|
| Quản trị viên | `admin@shop.vn` | `Admin@123` |
| Khách hàng | `khach@shop.vn` | `Khach@123` |
