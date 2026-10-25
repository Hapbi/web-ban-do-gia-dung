using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NovaShop.Models;

namespace NovaShop.Data;

public static class DbInitializer
{
    public static string Hash(string pwd)
    {
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes("NOVA_SHOP_2024_" + pwd)));
    }

    private static readonly HashSet<string> SeedProductSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
            "tu-lanh-samsung-488l",
            "tu-lanh-lg-617l",
            "tu-lanh-mini-toshiba-90l",
            "tu-lanh-sharp-323l",
            "may-giat-lg-9kg",
            "may-giat-samsung-10kg",
            "may-giat-say-electrolux",
            "may-giat-toshiba-8kg",
            "may-giat-bosch-8kg",
            "dieu-hoa-samsung-9000",
            "dieu-hoa-lg-12000",
            "dieu-hoa-panasonic-9000",
            "dieu-hoa-sharp-12000",
            "dieu-hoa-tu-dung-midea",
            "noi-com-sharp-18l",
            "noi-com-cao-tan-toshiba",
            "noi-com-panasonic-18l",
            "lo-vi-song-samsung-23l",
            "lo-nuong-sharp-32l",
            "lo-vi-song-toshiba-20l",
            "may-hut-bui-xiaomi-cam-tay",
            "robot-hut-bui-xiaomi",
            "may-hut-bui-samsung-1800w",
            "may-hut-bui-philips",
            "quat-dung-xiaomi",
            "quat-dieu-hoa-sharp",
            "quat-tran-panasonic",
            "quat-hop-toshiba",
            "may-loc-kk-xiaomi-4pro",
            "may-loc-kk-sharp-40m2",
            "may-loc-kk-philips",
            "bep-dien-doi-xiaomi",
            "bep-gas-am-bosch",
            "bep-tu-don-sharp",
            "may-hut-mui-electrolux",
            "noi-chien-philips-62l",
            "may-say-toc-philips",
            "ban-la-hoi-nuoc-philips",
            "may-xay-sinh-to-panasonic",
            "am-sieu-toc-toshiba",
            "noi-ap-suat-sharp",
            "may-rua-bat-bosch"
    };

    public static async Task InitializeAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        await DbSchemaUpgrader.EnsureAsync(db);

        if (!await db.SiteSettings.AnyAsync())
            db.SiteSettings.Add(new SiteSettings { SiteName = "BTVH", Tagline = "Điện máy & gia dụng cho mọi nhà", Email = "hotro@btvh.vn" });
        if (!await db.BankSettings.AnyAsync())
            db.BankSettings.Add(new BankSettings { BankName = "Vietcombank", Instructions = "Quản trị viên cần nhập đúng tài khoản ngân hàng trước khi bật nhận chuyển khoản." });
        if (!await db.Banners.AnyAsync())
            db.Banners.Add(new Banner { Title = "Khám phá gian hàng BTVH", ImageUrl = "/images/products/noi-chien-philips-62l.svg", LinkUrl = "/san-pham", Position = "home-top", SortOrder = 1, IsActive = true });
        await db.SaveChangesAsync();

        await RefreshSeedProductImagesAsync(db);
        if (await db.Users.AnyAsync()) return;

        // ============ USERS ============
        db.Users.AddRange(
            new User { Email = "admin@nova.vn", PasswordHash = Hash("admin123"), FullName = "Quản Trị Viên", Role = "Admin", Phone = "0900000001", Avatar = "A" },
            new User { Email = "staff@nova.vn", PasswordHash = Hash("staff123"), FullName = "Nhân Viên Bán Hàng", Role = "Staff", Phone = "0900000002", Avatar = "S", CanVerifyPayments = true },
            new User { Email = "customer@nova.vn", PasswordHash = Hash("customer123"), FullName = "Nguyễn Văn Khách", Role = "Customer", Phone = "0901234567", Avatar = "K" }
        );
        await db.SaveChangesAsync();

        // ============ CATEGORIES ============
        var cats = new[]
        {
            new Category { Name = "Tủ lạnh", Slug = "tu-lanh", Icon = "❄️", DisplayOrder = 1 },
            new Category { Name = "Máy giặt", Slug = "may-giat", Icon = "🌀", DisplayOrder = 2 },
            new Category { Name = "Điều hòa", Slug = "dieu-hoa", Icon = "🌡️", DisplayOrder = 3 },
            new Category { Name = "Nồi cơm", Slug = "noi-com", Icon = "🍚", DisplayOrder = 4 },
            new Category { Name = "Lò vi sóng", Slug = "lo-vi-song", Icon = "📻", DisplayOrder = 5 },
            new Category { Name = "Máy hút bụi", Slug = "may-hut-bui", Icon = "🧹", DisplayOrder = 6 },
            new Category { Name = "Quạt", Slug = "quat", Icon = "💨", DisplayOrder = 7 },
            new Category { Name = "Máy lọc KK", Slug = "may-loc-khong-khi", Icon = "🌿", DisplayOrder = 8 },
            new Category { Name = "Bếp", Slug = "bep", Icon = "🔥", DisplayOrder = 9 },
            new Category { Name = "Đồ gia dụng", Slug = "do-gia-dung", Icon = "🏠", DisplayOrder = 10 }
        };
        db.Categories.AddRange(cats);
        await db.SaveChangesAsync();

        // ============ BRANDS ============
        var brands = new[]
        {
            new Brand { Name = "Samsung", Slug = "samsung" },
            new Brand { Name = "LG", Slug = "lg" },
            new Brand { Name = "Panasonic", Slug = "panasonic" },
            new Brand { Name = "Sharp", Slug = "sharp" },
            new Brand { Name = "Toshiba", Slug = "toshiba" },
            new Brand { Name = "Electrolux", Slug = "electrolux" },
            new Brand { Name = "Philips", Slug = "philips" },
            new Brand { Name = "Xiaomi", Slug = "xiaomi" },
            new Brand { Name = "Midea", Slug = "midea" },
            new Brand { Name = "Bosch", Slug = "bosch" }
        };
        db.Brands.AddRange(brands);
        await db.SaveChangesAsync();

        // ============ PRODUCTS (42 sản phẩm mẫu) ============
        var P = new List<Product>();

        // Helper
        void AddProduct(string name, string slug, string shortDesc, string desc, int catIdx, int brandIdx,
            decimal original, decimal? sale, int stock, int sold, bool featured, string tags,
            params (string name, string value)[] specs)
        {
            var p = new Product
            {
                Name = name, Slug = slug, ShortDescription = shortDesc, Description = desc,
                CategoryId = cats[catIdx].Id, BrandId = brands[brandIdx].Id,
                OriginalPrice = original, SalePrice = sale, Stock = stock, SoldCount = sold,
                IsFeatured = featured, Tags = tags, ViewCount = sold * 3 + new Random().Next(50, 500)
            };
            // Product art is served locally so the catalog remains complete without third-party image hosts.
            var localImages = new[]
            {
                $"/images/products/{slug}.svg",
                $"/images/products/{slug}-angle.svg",
                $"/images/products/{slug}-detail.svg"
            };
            for (int i = 0; i < localImages.Length; i++)
                p.Images.Add(new ProductImage
                {
                    Url = localImages[i], Alt = $"{name} - {(i == 0 ? "ảnh chính" : i == 1 ? "góc nghiêng" : "chi tiết minh họa")}",
                    IsPrimary = i == 0, SortOrder = i
                });
            for (int i = 0; i < specs.Length; i++)
                p.Specs.Add(new ProductSpec { Name = specs[i].name, Value = specs[i].value, SortOrder = i });
            P.Add(p);
        }

        // ==== TỦ LẠNH (4) ====
        AddProduct("Tủ Lạnh Inverter Samsung 488L", "tu-lanh-samsung-488l", "Inverter tiết kiệm điện, ngăn đông mềm", "Tủ lạnh Samsung 4 cánh Inverter, dung tích 488L, công nghệ Digital Inverter tiết kiệm điện 40%, ngăn đông mềm Optimal Fresh+. Phù hợp gia đình 4-5 người. Bảo hành 24 tháng chính hãng.",
            0, 0, 18990000, 15490000, 14, 320, true, "inverter,tiết-kiệm,bestseller",
            ("Dung tích", "488 lít"), ("Công nghệ", "Digital Inverter"), ("Điện áp", "220V/50Hz"), ("Bảo hành", "24 tháng"), ("Kích thước", "83×72×178cm"), ("Màu sắc", "Xám bạc"));
        AddProduct("Tủ Lạnh Side-by-Side LG 617L", "tu-lanh-lg-617l", "Side-by-side, lấy nước ngoài", "Tủ lạnh LG Side-by-Side 617L, công nghệ Linear Inverter, lấy nước & đá ngoài, ngăn rau quả giữ tươi lâu, màn hình LED cảm ứng.",
            0, 1, 34900000, 29900000, 6, 145, true, "side-by-side,cao-cấp",
            ("Dung tích", "617 lít"), ("Công nghệ", "Linear Inverter"), ("Chức năng", "Lấy nước & đá"), ("Bảo hành", "24 tháng"));
        AddProduct("Tủ Lạnh Mini Toshiba 90L", "tu-lanh-mini-toshiba-90l", "Nhỏ gọn cho phòng trọ", "Tủ lạnh mini 1 cửa Toshiba 90L, phù hợp phòng trọ, văn phòng, khách sạn. Tiết kiệm điện, làm lạnh nhanh.",
            0, 4, 3590000, 2990000, 30, 540, false, "mini,phòng-trọ",
            ("Dung tích", "90 lít"), ("Điện áp", "220V"), ("Bảo hành", "12 tháng"));
        AddProduct("Tủ Lạnh 2 Cánh Sharp 323L", "tu-lanh-sharp-323l", "Ngăn đá trên, Inverter", "Tủ lạnh Sharp 2 cánh 323L, ngăn đá trên, công nghệ Inverter tiết kiệm điện, kháng khuẩn nano bạc.",
            0, 3, 9290000, 8290000, 18, 210, false, "2-cánh,kháng-khuẩn",
            ("Dung tích", "323 lít"), ("Công nghệ", "Inverter"), ("Bảo hành", "24 tháng"));

        // ==== MÁY GIẶT (5) ====
        AddProduct("Máy Giặt Cửa Trước LG 9kg", "may-giat-lg-9kg", "AI DD, Steam diệt khuẩn", "Máy giặt LG cửa trước 9kg, công nghệ AI DD giặt thông minh, Steam diệt khuẩn 99.9%, 14 chế độ giặt, Inverter tiết kiệm điện.",
            1, 1, 9990000, 8490000, 22, 410, true, "inverter,ai,bestseller",
            ("Khối lượng", "9 kg"), ("Công nghệ", "AI DD + Steam"), ("Tốc độ vắt", "1400 vòng/phút"), ("Bảo hành", "24 tháng"));
        AddProduct("Máy Giặt Samsung 10kg", "may-giat-samsung-10kg", "Cửa trên, EcoBubble", "Máy giặt Samsung 10kg cửa trên, công nghệ EcoBubble tạo bọt khí giặt sạch sâu, Inverter bền bỉ.",
            1, 0, 8490000, 7290000, 15, 320, false, "eco-bubble,cửa-trên",
            ("Khối lượng", "10 kg"), ("Công nghệ", "EcoBubble"), ("Bảo hành", "24 tháng"));
        AddProduct("Máy Giặt Sấy Electrolux 10/6kg", "may-giat-say-electrolux", "Giặt + sấy 2 trong 1", "Máy giặt sấy Electrolux 10kg giặt / 6kg sấy, công nghệ UltraMix, cảm biến SensiCare bảo vệ vải, Inverter tiết kiệm điện.",
            1, 5, 18990000, 16490000, 8, 95, true, "giặt-sấy,cao-cấp",
            ("Khối lượng", "10kg giặt / 6kg sấy"), ("Công nghệ", "UltraMix"), ("Bảo hành", "24 tháng"));
        AddProduct("Máy Giặt Toshiba 8kg", "may-giat-toshiba-8kg", "Cửa trên, giá tốt", "Máy giặt Toshiba 8kg cửa trên, công nghệ Great Waves xoáy 3 chiều, 8 chế độ giặt, tiết kiệm điện nước.",
            1, 4, 5290000, 4790000, 25, 380, false, "phổ-thông",
            ("Khối lượng", "8 kg"), ("Công nghệ", "Great Waves"), ("Bảo hành", "24 tháng"));
        AddProduct("Máy Giặt Bosch 8kg", "may-giat-bosch-8kg", "Cửa trước, sấy khô", "Máy giặt Bosch Serie 4 8kg, công nghệ EcoSilence Drive, 15 chế độ giặt, sấy khô hiệu quả.",
            1, 9, 13990000, 11990000, 10, 130, false, "bosch,cao-cấp",
            ("Khối lượng", "8 kg"), ("Công nghệ", "EcoSilence"), ("Bảo hành", "24 tháng"));

        // ==== ĐIỀU HÒA (5) ====
        AddProduct("Điều Hòa Samsung 1 Chiều 9.000 BTU", "dieu-hoa-samsung-9000", "Inverter, làm lạnh nhanh", "Điều hòa Samsung 1 chiều 9000 BTU, Inverter tiết kiệm điện 68%, lọc bụi mịn PM2.5, làm lạnh nhanh Digital Inverter Boost.",
            2, 0, 8490000, 6990000, 30, 520, true, "inverter,bestseller",
            ("Công suất", "9.000 BTU"), ("Loại", "1 chiều"), ("Công nghệ", "Digital Inverter"), ("Bảo hành", "24 tháng"));
        AddProduct("Điều Hòa LG 2 Chiều 12.000 BTU", "dieu-hoa-lg-12000", "2 chiều, Dual Inverter", "Điều hòa LG 2 chiều 12000 BTU, Dual Inverter tiết kiệm điện, làm lạnh nhanh, sưởi ấm mùa đông, lọc bụi mịn.",
            2, 1, 12990000, 10990000, 20, 340, true, "2-chiều,cao-cấp",
            ("Công suất", "12.000 BTU"), ("Loại", "2 chiều"), ("Công nghệ", "Dual Inverter"), ("Bảo hành", "24 tháng"));
        AddProduct("Điều Hòa Panasonic 1 Chiều 9.000 BTU", "dieu-hoa-panasonic-9000", "Nanoe-G, Inverter", "Điều hòa Panasonic 9000 BTU, công nghệ Nanoe-G lọc không khí, Inverter tiết kiệm điện, dàn đồng chống ăn mòn.",
            2, 2, 9990000, 8490000, 18, 290, false, "nanoe",
            ("Công suất", "9.000 BTU"), ("Công nghệ", "Nanoe-G"), ("Bảo hành", "24 tháng"));
        AddProduct("Điều Hòa Sharp 1 Chiều 12.000 BTU", "dieu-hoa-sharp-12000", "Inverter, giá tốt", "Điều hòa Sharp 12000 BTU Inverter, công nghệ Plasmacluster ion diệt khuẩn, tiết kiệm điện.",
            2, 3, 8490000, 7290000, 22, 410, false, "plasmacluster",
            ("Công suất", "12.000 BTU"), ("Công nghệ", "Plasmacluster"), ("Bảo hành", "24 tháng"));
        AddProduct("Điều Hòa Tủ Đứng Midea 24.000 BTU", "dieu-hoa-tu-dung-midea", "Cho phòng lớn, quán cafe", "Điều hòa tủ đứng Midea 24000 BTU, làm lạnh nhanh cho phòng lớn, quán cafe, nhà hàng.",
            2, 8, 24900000, 21900000, 5, 45, false, "tủ-đứng",
            ("Công suất", "24.000 BTU"), ("Loại", "Tủ đứng"), ("Bảo hành", "12 tháng"));

        // ==== NỒI CƠM (3) ====
        AddProduct("Nồi Cơm Điện Tử Sharp 1.8L", "noi-com-sharp-18l", "Nấu nhiều chế độ", "Nồi cơm điện tử Sharp 1.8L, lòng nồi chống dính, 10 chế độ nấu tự động (cơm trắng, cháo, nấu chậm, hấp...).",
            3, 3, 1890000, 1490000, 60, 900, true, "bestseller,đa-năng",
            ("Dung tích", "1.8 lít"), ("Công suất", "700W"), ("Chất liệu", "Hợp kim chống dính"), ("Bảo hành", "12 tháng"));
        AddProduct("Nồi Cơm Cao Tần Toshiba 1L", "noi-com-cao-tan-toshiba", "Cao tần IH, cơm ngon", "Nồi cơm cao tần Toshiba 1L, công nghệ IH gia nhiệt 3D, giữ ẩm, cơm chín đều và ngon.",
            3, 4, 4990000, 3990000, 15, 260, false, "cao-tần",
            ("Dung tích", "1 lít"), ("Công nghệ", "Cao tần IH"), ("Bảo hành", "12 tháng"));
        AddProduct("Nồi Cơm Điện Panasonic 1.8L", "noi-com-panasonic-18l", "Nắp gài, bền bỉ", "Nồi cơm điện Panasonic 1.8L nắp gài, lòng nồi chống dính, phù hợp gia đình 3-4 người.",
            3, 2, 1290000, 1090000, 40, 620, false, "phổ-thông",
            ("Dung tích", "1.8 lít"), ("Công suất", "650W"), ("Bảo hành", "12 tháng"));

        // ==== LÒ VI SÓNG (3) ====
        AddProduct("Lò Vi Sóng Samsung 23L Có Nướng", "lo-vi-song-samsung-23l", "Vi sóng + nướng", "Lò vi sóng Samsung 23L kết hợp nướng, 8 chế độ nấu, lòng lò tráng men dễ vệ sinh.",
            4, 0, 2490000, 1890000, 28, 330, true, "nướng",
            ("Dung tích", "23 lít"), ("Công suất", "900W"), ("Chức năng", "Vi sóng + Nướng"), ("Bảo hành", "12 tháng"));
        AddProduct("Lò Nướng Điện Sharp 32L", "lo-nuong-sharp-32l", "Nướng bánh, thịt", "Lò nướng điện Sharp 32L, 4 thanh nhiệt, nướng bánh, thịt, hải sản, điều chỉnh nhiệt độ 100-250°C.",
            4, 3, 2990000, 2490000, 12, 180, false, "nướng-bánh",
            ("Dung tích", "32 lít"), ("Công suất", "1500W"), ("Bảo hành", "12 tháng"));
        AddProduct("Lò Vi Sóng Toshiba 20L", "lo-vi-song-toshiba-20l", "Cơ bản, giá rẻ", "Lò vi sóng Toshiba 20L cơ bản, 5 mức công suất, hẹn giờ, phù hợp hâm nóng thức ăn.",
            4, 4, 1490000, 1090000, 35, 450, false, "cơ-bản",
            ("Dung tích", "20 lít"), ("Công suất", "700W"), ("Bảo hành", "12 tháng"));

        // ==== MÁY HÚT BỤI (4) ====
        AddProduct("Máy Hút Bụi Cầm Tay Xiaomi", "may-hut-bui-xiaomi-cam-tay", "Cầm tay không dây", "Máy hút bụi cầm tay Xiaomi không dây, lực hút mạnh 15000Pa, pin 30 phút, trọng lượng nhẹ 1.8kg, nhiều đầu hút.",
            5, 7, 3990000, 2990000, 20, 280, true, "không-dây,xiaomi",
            ("Lực hút", "15000 Pa"), ("Thời gian pin", "30 phút"), ("Dung tích", "0.5 lít"), ("Bảo hành", "12 tháng"));
        AddProduct("Robot Hút Bụi Lau Nhà Xiaomi", "robot-hut-bui-xiaomi", "Tự động lau hút", "Robot hút bụi lau nhà Xiaomi, dùng app điều khiển, cảm biến LDS, tự sạc, lau nhà tự động.",
            5, 7, 8990000, 6990000, 12, 180, true, "robot,thông-minh",
            ("Công nghệ", "Cảm biến LDS"), ("Pin", "5200mAh"), ("Diện tích", "250m²"), ("Bảo hành", "24 tháng"));
        AddProduct("Máy Hút Bụi Samsung 1800W", "may-hut-bui-samsung-1800w", "Dạng hộp, có dây", "Máy hút bụi Samsung 1800W dạng hộp, lực hút mạnh, bình chứa 2L, dây dài 5m.",
            5, 0, 2990000, 2290000, 18, 340, false, "dạng-hộp",
            ("Công suất", "1800W"), ("Bình chứa", "2 lít"), ("Bảo hành", "12 tháng"));
        AddProduct("Máy Hút Bụi Cầm Tay Philips", "may-hut-bui-philips", "Nhỏ gọn, tiện lợi", "Máy hút bụi cầm tay Philips nhỏ gọn, sạc pin, phù hợp hút bụi xe hơi, giường, sofa.",
            5, 6, 2490000, 1890000, 25, 220, false, "cầm-tay",
            ("Công suất", "85W"), ("Thời gian pin", "20 phút"), ("Bảo hành", "12 tháng"));

        // ==== QUẠT (4) ====
        AddProduct("Quạt Đứng Xiaomi Điều Khiển Từ Xa", "quat-dung-xiaomi", "3 tốc độ, remote", "Quạt đứng Xiaomi 3 tốc độ, điều khiển từ xa, hẹn giờ, chế độ ngủ đêm, tiết kiệm điện.",
            6, 7, 1290000, 990000, 70, 800, true, "bestseller,remote",
            ("Loại", "Quạt đứng"), ("Công suất", "45W"), ("Tốc độ", "3 tốc độ"), ("Điều khiển", "Remote + Hẹn giờ"));
        AddProduct("Quạt Điều Hòa Hơi Nước Sharp", "quat-dieu-hoa-sharp", "Làm mát bằng hơi nước", "Quạt điều hòa hơi nước Sharp, bình nước 40L, làm mát phòng 30m², 3 tốc độ gió, remote.",
            6, 3, 3990000, 3290000, 16, 140, false, "hơi-nước",
            ("Công suất", "85W"), ("Bình nước", "40L"), ("Diện tích", "30m²"), ("Độ ồn", "≤55dB"));
        AddProduct("Quạt Trần Panasonic 5 Cánh", "quat-tran-panasonic", "Trang trí, mát mẻ", "Quạt trần Panasonic 5 cánh, công suất 75W, có remote, tiết kiệm điện, làm mát phòng 30-40m².",
            6, 2, 3290000, 2790000, 22, 175, false, "trần",
            ("Loại", "Quạt trần"), ("Công suất", "75W"), ("Bảo hành", "12 tháng"));
        AddProduct("Quạt Hộp Để Bàn Toshiba", "quat-hop-toshiba", "Nhỏ gọn, tiết kiệm", "Quạt hộp để bàn Toshiba nhỏ gọn, 3 tốc độ, tiết kiệm điện, phù hợp bàn làm việc, giường ngủ.",
            6, 4, 590000, 450000, 120, 1500, false, "bàn,tiết-kiệm",
            ("Loại", "Quạt hộp"), ("Công suất", "35W"), ("Bảo hành", "12 tháng"));

        // ==== MÁY LỌC KK (3) ====
        AddProduct("Máy Lọc Không Khí Xiaomi 4 Pro", "may-loc-kk-xiaomi-4pro", "Lọc bụi mịn PM2.5", "Máy lọc không khí Xiaomi 4 Pro, màng HEPA H13, lọc bụi mịn PM2.5, diện tích 60m², điều khiển app.",
            7, 7, 5990000, 4990000, 11, 175, true, "hepa,xiaomi",
            ("Diện tích", "60 m²"), ("Màng lọc", "HEPA H13"), ("Công suất", "55W"), ("Độ ồn", "<35 dB"));
        AddProduct("Máy Lọc Không Khí Sharp 40m²", "may-loc-kk-sharp-40m2", "Plasmacluster ion", "Máy lọc không khí Sharp 40m², công nghệ Plasmacluster ion diệt khuẩn, lọc bụi mịn, khử mùi.",
            7, 3, 4990000, 3990000, 15, 220, false, "plasmacluster",
            ("Diện tích", "40 m²"), ("Công nghệ", "Plasmacluster"), ("Bảo hành", "24 tháng"));
        AddProduct("Máy Lọc Không Khí Philips", "may-loc-kk-philips", "Chống dị ứng", "Máy lọc không khí Philips, màng HEPA, than hoạt tính, phù hợp phòng ngủ, chống dị ứng.",
            7, 6, 6990000, 5490000, 9, 130, false, "philips",
            ("Diện tích", "50 m²"), ("Màng lọc", "HEPA + Carbon"), ("Bảo hành", "24 tháng"));

        // ==== BẾP (4) ====
        AddProduct("Bếp Điện Đôi Xiaomi", "bep-dien-doi-xiaomi", "Bếp từ đôi, Inverter", "Bếp điện đôi Xiaomi, mặt kính cường lực, 9 mức công suất, hẹn giờ, an toàn, tiết kiệm điện.",
            8, 7, 3990000, 2990000, 18, 195, true, "bếp-từ",
            ("Số bếp", "2 bếp"), ("Công suất", "4000W"), ("Mặt bếp", "Kính cường lực"), ("Bảo hành", "24 tháng"));
        AddProduct("Bếp Gas Âm Bosch 2 Bếp", "bep-gas-am-bosch", "Bếp gas âm, an toàn", "Bếp gas âm Bosch 2 bếp, mặt kính cường lực, đánh lửa điện tử, cảm biến an toàn.",
            8, 9, 6990000, 5990000, 10, 95, false, "gas",
            ("Số bếp", "2 bếp"), ("Loại", "Bếp gas âm"), ("Bảo hành", "24 tháng"));
        AddProduct("Bếp Từ Đơn Sharp", "bep-tu-don-sharp", "1 bếp, tiện lợi", "Bếp từ đơn Sharp, 1 bếp, công suất 2000W, mặt kính cường lực, phù hợp căn hộ nhỏ.",
            8, 3, 1990000, 1490000, 25, 280, false, "đơn",
            ("Số bếp", "1 bếp"), ("Công suất", "2000W"), ("Bảo hành", "12 tháng"));
        AddProduct("Máy Hút Mùi Electrolux", "may-hut-mui-electrolux", "Hút mùi bếp, Inverter", "Máy hút mùi Electrolux, công suất hút 1000m³/h, Inverter, đèn LED, 3 tốc độ.",
            8, 5, 4990000, 4290000, 12, 85, false, "hút-mùi",
            ("Công suất", "1000 m³/h"), ("Tốc độ", "3 tốc độ"), ("Bảo hành", "24 tháng"));

        // ==== ĐỒ GIA DỤNG (7) ====
        AddProduct("Nồi Chiên Không Dầu Philips 6.2L", "noi-chien-philips-62l", "Rapid Air, ít dầu mỡ", "Nồi chiên không dầu Philips 6.2L, công nghệ Rapid Air, 7 chế độ nấu, ít dầu mỡ, sạch sẽ.",
            9, 6, 5990000, 4590000, 20, 180, true, "chiên-không-dầu,bestseller",
            ("Dung tích", "6.2 lít"), ("Công suất", "2000W"), ("Chế độ", "7 chế độ"), ("Bảo hành", "24 tháng"));
        AddProduct("Máy Sấy Tóc Philips 1800W", "may-say-toc-philips", "Ion âm bảo vệ tóc", "Máy sấy tóc Philips 1800W, ion âm bảo vệ tóc, 3 mức nhiệt, 2 tốc độ, gấp gọn.",
            9, 6, 890000, 690000, 40, 480, false, "ion-âm",
            ("Công suất", "1800W"), ("Chế độ", "3 nhiệt + 2 tốc độ"), ("Bảo hành", "12 tháng"));
        AddProduct("Bàn Là Hơi Nước Philips", "ban-la-hoi-nuoc-philips", "Công suất mạnh", "Bàn là hơi nước Philips 2400W, hơi nước liên tục 40g/phút, bình nước 2L, làm nóng 45s.",
            9, 6, 3290000, 2590000, 25, 280, false, "hơi-nước",
            ("Công suất", "2400W"), ("Bình nước", "2 lít"), ("Bảo hành", "24 tháng"));
        AddProduct("Máy Xay Sinh Tố Panasonic", "may-xay-sinh-to-panasonic", "Xay đa năng", "Máy xay sinh tố Panasonic, 3 cối, công suất 600W, lưỡi thép không gỉ, xay đá, sinh tố.",
            9, 2, 1490000, 1090000, 35, 620, false, "sinh-tố",
            ("Công suất", "600W"), ("Số cối", "3 cối"), ("Bảo hành", "12 tháng"));
        AddProduct("Ấm Siêu Tốc Toshiba 1.7L", "am-sieu-toc-toshiba", "Đun nước nhanh", "Ấm siêu tốc Toshiba 1.7L, inox 304, đun sôi nhanh, tự ngắt điện, chống khô.",
            9, 4, 690000, 490000, 60, 890, false, "siêu-tốc",
            ("Dung tích", "1.7 lít"), ("Chất liệu", "Inox 304"), ("Bảo hành", "12 tháng"));
        AddProduct("Nồi Áp Suất Điện Tử Sharp", "noi-ap-suat-sharp", "Nấu đa năng", "Nồi áp suất điện tử Sharp, dung tích 5L, 12 chế độ nấu, van an toàn, nấu nhanh.",
            9, 3, 2990000, 2490000, 18, 165, false, "áp-suất",
            ("Dung tích", "5 lít"), ("Công suất", "900W"), ("Bảo hành", "12 tháng"));
        AddProduct("Máy Rửa Bát Bosch 12 Bộ", "may-rua-bat-bosch", "Rửa sạch, tiết kiệm", "Máy rửa bát Bosch Series 4, 12 bộ, công nghệ sấy khô Condensation, tiết kiệm điện nước.",
            9, 9, 18900000, 15900000, 6, 45, true, "rửa-bát,cao-cấp",
            ("Dung tích", "12 bộ"), ("Công suất", "1800W"), ("Bảo hành", "24 tháng"));

        db.Products.AddRange(P);
        await db.SaveChangesAsync();

        // A separate inactive demo listing gives administrators a real moderation example without polluting the public catalog.
        var demoSeller = await db.Users.FirstAsync(u => u.Email == "customer@nova.vn");
        if (!await db.Products.AnyAsync(p => p.IsMarketplaceListing))
        {
            var listing = new Product
            {
                Name = "Robot hút bụi đã qua sử dụng (listing demo)", Slug = "marketplace-robot-hut-bui-demo",
                ShortDescription = "Tin đăng mẫu để thử quy trình kiểm duyệt Marketplace.",
                Description = "Đây là listing demo, không phải hàng hóa được xác minh. Quản trị viên có thể duyệt hoặc từ chối để thử luồng kiểm duyệt.",
                CategoryId = cats[5].Id, BrandId = brands[7].Id, OwnerUserId = demoSeller.Id,
                OriginalPrice = 1800000m, Stock = 1, IsActive = false, IsMarketplaceListing = true,
                ListingStatus = ListingStatus.Pending, PlatformFeeRate = 0.07m, CreatedAt = DateTime.Now
            };
            listing.Images.Add(new ProductImage { Url = "/images/products/robot-hut-bui-xiaomi.svg", Alt = "Ảnh minh họa listing Marketplace demo", IsPrimary = true, SortOrder = 0 });
            listing.Specs.Add(new ProductSpec { Name = "Tình trạng", Value = "Tin đăng minh họa", SortOrder = 0 });
            db.Products.Add(listing);
            await db.SaveChangesAsync();
        }

        // ============ COUPONS ============
        db.Coupons.AddRange(
            new Coupon { Code = "WELCOME10", Description = "Giảm 10% đơn đầu tiên", Type = DiscountType.Percentage, Value = 10, MinOrderAmount = 500000, MaxDiscount = 500000, UsageLimit = 1000, IsActive = true },
            new Coupon { Code = "BTVH200K", Description = "Giảm 200.000đ cho đơn từ 2 triệu", Type = DiscountType.FixedAmount, Value = 200000, MinOrderAmount = 2000000, UsageLimit = 500, IsActive = true },
            new Coupon { Code = "FREESHIP", Description = "Miễn phí vận chuyển", Type = DiscountType.FixedAmount, Value = 30000, MinOrderAmount = 0, UsageLimit = 2000, IsActive = true },
            new Coupon { Code = "SALE20", Description = "Giảm 20% đơn từ 5 triệu", Type = DiscountType.Percentage, Value = 20, MinOrderAmount = 5000000, MaxDiscount = 2000000, UsageLimit = 200, IsActive = true }
        );

        // ============ BLOG POSTS ============
        db.BlogPosts.AddRange(
            new BlogPost { Title = "5 mẹo chọn mua tủ lạnh tiết kiệm điện", Slug = "5-meo-chon-tu-lanh", Summary = "Chọn tủ lạnh đúng cách để tiết kiệm điện năng và kéo dài tuổi thọ.", Content = "1. Chọn công suất phù hợp với số người trong gia đình.\n2. Ưu tiên công nghệ Inverter.\n3. Kiểm tra nhãn năng lượng.\n4. Chọn vị trí đặt tủ thoáng mát.\n5. Vệ sinh tủ định kỳ.", CoverImage = "/images/editorial/fridge-care.svg", AuthorName = "Admin", Category = "Hướng dẫn", IsPublished = true, PublishedAt = DateTime.Now.AddDays(-5), ViewCount = 234 },
            new BlogPost { Title = "So sánh máy giặt cửa trước và cửa trên", Slug = "so-sanh-may-giat-cua-truoc-cua-tren", Summary = "Đâu là lựa chọn tốt hơn cho gia đình bạn?", Content = "Máy giặt cửa trước giặt sạch hơn, tiết kiệm nước, nhưng giá cao và thời gian giặt lâu hơn. Máy giặt cửa trên rẻ hơn, giặt nhanh hơn nhưng tốn nước hơn.", CoverImage = "/images/editorial/washer-guide.svg", AuthorName = "Admin", Category = "So sánh", IsPublished = true, PublishedAt = DateTime.Now.AddDays(-3), ViewCount = 189 },
            new BlogPost { Title = "Cách vệ sinh điều hòa đúng cách", Slug = "cach-ve-sinh-dieu-hoa", Summary = "Vệ sinh điều hòa định kỳ để tăng hiệu quả làm mát và tiết kiệm điện.", Content = "1. Vệ sinh filter 2 tuần/lần.\n2. Vệ sinh dàn lạnh 3 tháng/lần.\n3. Kiểm tra gas 6 tháng/lần.\n4. Bảo dưỡng tổng thể 1 năm/lần.", CoverImage = "/images/editorial/ac-care.svg", AuthorName = "Admin", Category = "Hướng dẫn", IsPublished = true, PublishedAt = DateTime.Now.AddDays(-1), ViewCount = 156 }
        );
        await db.SaveChangesAsync();

        // ============ ORDERS MẪU ============
        var customer = await db.Users.FirstAsync(u => u.Email == "customer@nova.vn");
        var now = DateTime.Now;

        var sampleOrders = new[]
        {
            new Order {
                Code = "DH241015ABCD", UserId = customer.Id, CustomerName = "Nguyễn Văn Khách", Phone = "0901234567", Email = "customer@nova.vn",
                Address = "123 Lê Lợi, Phường Bến Thành, Quận 1, TP.HCM",
                Subtotal = 15490000, Discount = 0, Shipping = 0, Total = 15490000,
                Status = OrderStatus.Delivered, Payment = PaymentStatus.Paid, PaymentMethod = PaymentMethod.BankTransfer,
                CreatedAt = now.AddDays(-15), ConfirmedAt = now.AddDays(-14), ShippedAt = now.AddDays(-13), DeliveredAt = now.AddDays(-11)
            },
            new Order {
                Code = "DH241020EFGH", UserId = customer.Id, CustomerName = "Nguyễn Văn Khách", Phone = "0901234567", Email = "customer@nova.vn",
                Address = "123 Lê Lợi, Phường Bến Thành, Quận 1, TP.HCM",
                Subtotal = 16980000, Discount = 200000, CouponCode = "BTVH200K", Shipping = 0, Total = 16780000,
                Status = OrderStatus.Shipping, Payment = PaymentStatus.Paid, PaymentMethod = PaymentMethod.BankTransfer,
                CreatedAt = now.AddDays(-5), ConfirmedAt = now.AddDays(-4), ShippedAt = now.AddDays(-2)
            },
            new Order {
                Code = "DH241025IJKL", UserId = customer.Id, CustomerName = "Nguyễn Văn Khách", Phone = "0901234567", Email = "customer@nova.vn",
                Address = "123 Lê Lợi, Phường Bến Thành, Quận 1, TP.HCM",
                Subtotal = 2480000, Discount = 0, Shipping = 30000, Total = 2510000,
                Status = OrderStatus.Confirmed, Payment = PaymentStatus.Paid, PaymentMethod = PaymentMethod.COD,
                CreatedAt = now.AddDays(-2), ConfirmedAt = now.AddDays(-1)
            },
            new Order {
                Code = "DH241028MNOP", UserId = customer.Id, CustomerName = "Nguyễn Văn Khách", Phone = "0901234567", Email = "customer@nova.vn",
                Address = "123 Lê Lợi, Phường Bến Thành, Quận 1, TP.HCM",
                Subtotal = 2990000, Discount = 0, Shipping = 30000, Total = 3020000,
                Status = OrderStatus.Pending, Payment = PaymentStatus.Unpaid, PaymentMethod = PaymentMethod.COD,
                CreatedAt = now.AddHours(-6)
            }
        };

        // Add items to each order
        sampleOrders[0].Items.Add(new OrderItem { ProductId = P[0].Id, ProductName = P[0].Name, ProductImage = P[0].PrimaryImage, ProductSlug = P[0].Slug, UnitPrice = P[0].CurrentPrice, OriginalUnitPrice = P[0].OriginalPrice, Quantity = 1 });
        sampleOrders[1].Items.Add(new OrderItem { ProductId = P[5].Id, ProductName = P[5].Name, ProductImage = P[5].PrimaryImage, ProductSlug = P[5].Slug, UnitPrice = P[5].CurrentPrice, OriginalUnitPrice = P[5].OriginalPrice, Quantity = 2 });
        sampleOrders[2].Items.Add(new OrderItem { ProductId = P[16].Id, ProductName = P[16].Name, ProductImage = P[16].PrimaryImage, ProductSlug = P[16].Slug, UnitPrice = P[16].CurrentPrice, OriginalUnitPrice = P[16].OriginalPrice, Quantity = 2 });
        sampleOrders[3].Items.Add(new OrderItem { ProductId = P[13].Id, ProductName = P[13].Name, ProductImage = P[13].PrimaryImage, ProductSlug = P[13].Slug, UnitPrice = P[13].CurrentPrice, OriginalUnitPrice = P[13].OriginalPrice, Quantity = 1 });

        foreach (var o in sampleOrders)
        {
            o.History.Add(new OrderStatusHistory { Status = OrderStatus.Pending, Note = "Đơn hàng được tạo", ChangedBy = "Customer", CreatedAt = o.CreatedAt });
            if (o.ConfirmedAt.HasValue) o.History.Add(new OrderStatusHistory { Status = OrderStatus.Confirmed, Note = "Đã xác nhận đơn", ChangedBy = "Staff", CreatedAt = o.ConfirmedAt.Value });
            if (o.ShippedAt.HasValue) o.History.Add(new OrderStatusHistory { Status = OrderStatus.Shipping, Note = "Đang giao hàng", ChangedBy = "Staff", CreatedAt = o.ShippedAt.Value });
            if (o.DeliveredAt.HasValue) o.History.Add(new OrderStatusHistory { Status = OrderStatus.Delivered, Note = "Đã giao hàng thành công", ChangedBy = "Staff", CreatedAt = o.DeliveredAt.Value });
        }
        db.Orders.AddRange(sampleOrders);

        // ============ SAMPLE REVIEWS ============
        db.Reviews.AddRange(
            new Review { ProductId = P[0].Id, UserId = customer.Id, Rating = 5, Title = "Tuyệt vời", Content = "Tủ lạnh rất đẹp, tiết kiệm điện, giao hàng nhanh. Rất hài lòng!", IsVerifiedPurchase = true, Status = ReviewStatus.Approved, CreatedAt = now.AddDays(-10) },
            new Review { ProductId = P[0].Id, UserId = customer.Id, Rating = 4, Title = "Tốt", Content = "Chất lượng tốt, giá hợp lý. Chỉ tiếc là hơi ồn khi chạy.", IsVerifiedPurchase = true, Status = ReviewStatus.Approved, CreatedAt = now.AddDays(-8) },
            new Review { ProductId = P[5].Id, UserId = customer.Id, Rating = 5, Title = "Đáng tiền", Content = "Máy giặt LG giặt rất sạch, tiết kiệm điện, ít ồn. Recommend!", IsVerifiedPurchase = true, Status = ReviewStatus.Approved, CreatedAt = now.AddDays(-7) },
            new Review { ProductId = P[13].Id, UserId = customer.Id, Rating = 5, Title = "Ngon cơm", Content = "Nồi cơm nấu ngon, giữ nóng tốt, giá cả phải chăng.", IsVerifiedPurchase = true, Status = ReviewStatus.Approved, CreatedAt = now.AddDays(-5) }
        );

        await db.SaveChangesAsync();
    }
    private static async Task RefreshSeedProductImagesAsync(AppDbContext db)
    {
        var products = await db.Products.Include(p => p.Images).ToListAsync();
        var changed = false;
        foreach (var product in products.Where(p => SeedProductSlugs.Contains(p.Slug)))
        {
            var desired = new[]
            {
                $"/images/products/{product.Slug}.svg",
                $"/images/products/{product.Slug}-angle.svg",
                $"/images/products/{product.Slug}-detail.svg"
            };
            var images = product.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder).ToList();
            for (var i = 0; i < desired.Length; i++)
            {
                if (i < images.Count)
                {
                    var image = images[i];
                    var alt = $"{product.Name} - {(i == 0 ? "ảnh chính" : i == 1 ? "góc nghiêng" : "chi tiết minh họa")}";
                    if (!string.Equals(image.Url, desired[i], StringComparison.Ordinal)
                        || !string.Equals(image.Alt, alt, StringComparison.Ordinal)
                        || image.IsPrimary != (i == 0) || image.SortOrder != i)
                    {
                        image.Url = desired[i];
                        image.Alt = alt;
                        image.IsPrimary = i == 0;
                        image.SortOrder = i;
                        changed = true;
                    }
                }
                else
                {
                    product.Images.Add(new ProductImage
                    {
                        Url = desired[i], Alt = $"{product.Name} - {(i == 0 ? "ảnh chính" : i == 1 ? "góc nghiêng" : "chi tiết minh họa")}",
                        IsPrimary = i == 0, SortOrder = i
                    });
                    changed = true;
                }
            }
            foreach (var extra in images.Skip(desired.Length))
            {
                db.ProductImages.Remove(extra);
                changed = true;
            }
        }
        var seedProductSlugs = SeedProductSlugs.ToArray();
        var sampleOrderItems = await db.OrderItems.Where(i => seedProductSlugs.Contains(i.ProductSlug)).ToListAsync();
        foreach (var item in sampleOrderItems)
        {
            var localOrderImage = $"/images/products/{item.ProductSlug}.svg";
            if (!string.Equals(item.ProductImage, localOrderImage, StringComparison.Ordinal))
            {
                item.ProductImage = localOrderImage;
                changed = true;
            }
        }

        var editorialCovers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["5-meo-chon-tu-lanh"] = "/images/editorial/fridge-care.svg",
            ["so-sanh-may-giat-cua-truoc-cua-tren"] = "/images/editorial/washer-guide.svg",
            ["cach-ve-sinh-dieu-hoa"] = "/images/editorial/ac-care.svg"
        };
        var editorialSlugs = editorialCovers.Keys.ToArray();
        var posts = await db.BlogPosts.Where(p => editorialSlugs.Contains(p.Slug)).ToListAsync();
        foreach (var post in posts)
        {
            var localCover = editorialCovers[post.Slug];
            if (!string.Equals(post.CoverImage, localCover, StringComparison.Ordinal))
            {
                post.CoverImage = localCover;
                changed = true;
            }
        }
        if (changed) await db.SaveChangesAsync();
    }

}


