<div align="center">

# 🏠 GIADUNGCHATLUONG
### Website bán đồ gia dụng chất lượng – Quản lý bán hàng toàn diện

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC-5C2D91)
![EF Core](https://img.shields.io/badge/Entity%20Framework-Core%208-blue)
![SQLite](https://img.shields.io/badge/Database-SQLite%20%7C%20SQL%20Server-003B57?logo=sqlite&logoColor=white)
![JavaScript](https://img.shields.io/badge/JavaScript-ES6-F7DF1E?logo=javascript&logoColor=black)

</div>

---

## 📖 Giới thiệu

**GIADUNGCHATLUONG** là website thương mại điện tử bán đồ gia dụng, gồm hai phần:

- 🛍️ **Khu vực khách hàng:** xem – tìm – mua sản phẩm, đánh giá, theo dõi đơn hàng.
- 🛠️ **Khu vực quản trị:** quản lý sản phẩm, kho, đơn hàng, doanh thu, người dùng, phân quyền và giám sát hệ thống.

Dự án áp dụng mô hình **MVC**, tách rõ 3 lớp **Frontend – Backend – Database** để dễ bảo trì và mở rộng.

---

## ✨ Chức năng chính

### 🛍️ Dành cho khách hàng

| Nhóm | Chức năng |
|---|---|
| **Trang chủ** | Banner quảng cáo, danh mục nổi bật, sản phẩm bán chạy / mới / nổi bật, bảng xếp hạng khách hàng |
| **Sản phẩm** | Tìm kiếm theo từ khóa, lọc theo danh mục và khoảng giá, sắp xếp (giá tăng/giảm, bán chạy), phân trang |
| **Chi tiết sản phẩm** | Mô tả, công dụng, đối tượng phù hợp, tính năng nổi bật, lưu ý sử dụng, sản phẩm liên quan |
| **Đánh giá** | Chỉ khách đã mua và nhận hàng mới được đánh giá; đánh giá được quản trị viên duyệt trước khi hiển thị |
| **Yêu thích** | Lưu sản phẩm yêu thích, xem lại danh sách cá nhân |
| **Giỏ hàng** | Thêm / sửa số lượng / xóa từng món / xóa toàn bộ, cập nhật tức thì bằng Fetch API (không tải lại trang) |
| **Mua ngay** | Luồng riêng, không lẫn với giỏ hàng; chưa đăng nhập sẽ được chuyển đăng nhập rồi quay lại bước thanh toán |
| **Thanh toán** | Hỗ trợ **COD** và **chuyển khoản ngân hàng**; kiểm tra họ tên, số điện thoại, địa chỉ |
| **Đơn hàng** | Xem danh sách và chi tiết đơn của mình, theo dõi trạng thái: *Chờ xác nhận → Đã xác nhận → Đang giao → Hoàn thành / Đã hủy* |
| **Tài khoản** | Đăng ký, đăng nhập, đăng xuất, đổi mật khẩu |
| **Liên hệ** | Gửi góp ý / yêu cầu hỗ trợ tới cửa hàng |
| **🏆 Bảng xếp hạng** | Xếp hạng Top 20 khách mua nhiều nhất, tích điểm (10.000đ = 1 điểm), 3 hạng đầu nhận quà do cửa hàng cấu hình |

### 🛠️ Dành cho quản trị

| Nhóm | Chức năng |
|---|---|
| **📊 Dashboard** | Doanh thu hôm nay / tháng, số đơn theo từng trạng thái, tổng khách hàng, giá trị tồn kho, cảnh báo sản phẩm **sắp hết / hết hàng**, đơn mới nhất, Top khách hàng |
| **Sản phẩm** | Thêm / sửa / xóa, ẩn – hiện sản phẩm, tìm kiếm, phân trang |
| **Danh mục** | Quản lý nhóm sản phẩm |
| **Kho hàng** | Theo dõi tồn kho, lọc theo tình trạng |
| **Đơn hàng** | Lọc – tìm kiếm đơn, cập nhật trạng thái, xác nhận thanh toán |
| **Đánh giá** | Duyệt hoặc xóa đánh giá của khách |
| **Liên hệ** | Tiếp nhận, ghi chú và cập nhật trạng thái xử lý |
| **Nội dung** | Quản lý banner trang chủ và bài viết |
| **📈 Thống kê** | Báo cáo doanh thu theo khoảng ngày, **xuất file CSV** |
| **👥 Người dùng** | Khóa / mở khóa tài khoản, đổi vai trò, đặt lại mật khẩu, thêm nhân viên |
| **🔐 Phân quyền chi tiết** | 4 vai trò (Admin, Quản trị phụ, Nhân viên, Khách hàng) và **9 quyền** cấp riêng cho từng tài khoản |
| **⚙️ Cấu hình cửa hàng** | Thông tin shop, tài khoản ngân hàng nhận tiền, quà bảng xếp hạng |
| **🎨 Tùy chỉnh giao diện** | Đổi màu chủ đạo, kiểu chữ, số cột sản phẩm, kiểu thẻ, độ rộng trang; **sắp xếp / ẩn hiện các khối trang chủ** ngay trong trang quản trị |
| **📝 Nhật ký hệ thống** | Ghi lại hành động người dùng, nhật ký truy cập chi tiết (phương thức, đường dẫn, IP), nhật ký theo từng người kèm **biểu đồ** |

---

## 🔒 Bảo mật & độ tin cậy

- 🔑 **Mật khẩu được băm bằng PBKDF2-SHA256** (210.000 vòng lặp, salt ngẫu nhiên), không lưu mật khẩu thô; hỗ trợ nâng cấp hash cũ.
- 🍪 **Cookie Authentication** (HttpOnly, SameSite) và kiểm tra lại trạng thái khóa / vai trò ở mỗi yêu cầu, nên tài khoản bị khóa hoặc bị thu quyền sẽ mất hiệu lực ngay.
- 🛡️ **Phân quyền theo vai trò và theo từng chức năng** bằng `[Authorize]` và `QuyenHelper`.
- 🧾 **Đặt hàng an toàn:** hệ thống đọc lại giá và tồn kho từ CSDL trước khi tạo đơn; tạo đơn và trừ kho nằm trong **một giao dịch (transaction)**.
- ↩️ Chỉ chấp nhận `returnUrl` nội bộ hợp lệ, tránh chuyển hướng ra ngoài.

---

## 🧰 Công nghệ sử dụng

| Thành phần | Công nghệ |
|---|---|
| Backend | C#, ASP.NET Core MVC (.NET 8) |
| Frontend | Razor (.cshtml), HTML5, CSS3, JavaScript (Fetch API, Canvas) |
| ORM | Entity Framework Core 8 |
| Cơ sở dữ liệu | SQLite (mặc định) – có thể chuyển sang SQL Server qua `appsettings.json` |
| Xác thực | Cookie Authentication + Session |

---

## 📂 Cấu trúc dự án

```text
GIADUNGCHATLUONG/
├── Frontend/     # Giao diện: trang Razor (GiaoDien) + CSS/JS/hình ảnh (TaiNguyen)
├── Backend/      # Xử lý: DieuKhien (Controller), MoHinh (Model), TienIch (Helper), Program.cs
├── Database/     # AppDb (EF Core), tạo dữ liệu mẫu, tệp cua-hang.db
└── appsettings.json
```

---

## 🚀 Cách chạy

1. Cài **.NET 8 SDK**.
2. Mở thư mục `GIADUNGCHATLUONG`, nhấp đúp `CHAY_GIADUNGCHATLUONG.bat` (hoặc mở `.csproj` bằng Visual Studio rồi nhấn Run).
3. Truy cập địa chỉ hiển thị trong cửa sổ chạy.

### 👤 Tài khoản demo

| Vai trò | Email | Mật khẩu |
|---|---|---|
| Quản trị viên | `admin@shop.vn` | `Admin@123` |
| Nhân viên | `staff@shop.vn` | `Staff@123` |
| Khách hàng | `khach@shop.vn` | `Khach@123` |
| Quản trị phụ | `adminphu@shop.vn` | `AdminPhu@123` |

