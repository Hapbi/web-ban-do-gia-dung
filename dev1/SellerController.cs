using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovaShop.Data;
using NovaShop.Models;
using NovaShop.Services;

namespace NovaShop.Controllers;

[Authorize]
public class SellerController : Controller
{
    private readonly AppDbContext _db;
    private readonly AuthService _auth;
    private readonly SettingsService _settings;
    private readonly AuditService _audit;
    private readonly IWebHostEnvironment _environment;

    public SellerController(AppDbContext db, AuthService auth, SettingsService settings, AuditService audit, IWebHostEnvironment environment)
    { _db = db; _auth = auth; _settings = settings; _audit = audit; _environment = environment; }

    [HttpGet, Route("kenh-ban-hang")]
    public async Task<IActionResult> Index()
    {
        var user = await _auth.GetCurrentAsync();
        if (user == null) return Challenge();
        var listings = await _db.Products.AsNoTracking()
            .Include(p => p.Images).Include(p => p.Category).Include(p => p.Brand)
            .Where(p => p.IsMarketplaceListing && p.OwnerUserId == user.Id)
            .OrderByDescending(p => p.CreatedAt).ToListAsync();
        return View(new SellerListingVM { Listings = listings });
    }

    [HttpGet, Route("kenh-ban-hang/dang-san-pham")]
    public async Task<IActionResult> Create()
    {
        var vm = new SellerCreateVM();
        await LoadOptionsAsync(vm);
        return View("Create", vm);
    }

    [HttpGet, Route("kenh-ban-hang/{id:int}/sua")]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _auth.GetCurrentAsync();
        if (user == null) return Challenge();
        var product = await _db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id && p.IsMarketplaceListing && p.OwnerUserId == user.Id);
        if (product == null) return NotFound();
        if (product.ListingStatus == ListingStatus.Approved)
        {
            TempData["Error"] = "Tin đã được duyệt. Hãy liên hệ quản trị viên nếu cần chỉnh sửa thông tin.";
            return RedirectToAction(nameof(Index));
        }
        var vm = new SellerCreateVM
        {
            Id = product.Id, Name = product.Name, Brand = (await _db.Brands.FindAsync(product.BrandId))?.Name ?? "",
            Price = product.OriginalPrice, Stock = product.Stock, CategoryId = product.CategoryId,
            Description = product.Description, ImageUrl = product.Images.OrderByDescending(i => i.IsPrimary).FirstOrDefault()?.Url
        };
        await LoadOptionsAsync(vm);
        return View("Create", vm);
    }

    [HttpPost, Route("kenh-ban-hang/luu")]
    public async Task<IActionResult> Save(SellerCreateVM vm)
    {
        var user = await _auth.GetCurrentAsync();
        if (user == null) return Challenge();
        await LoadOptionsAsync(vm);
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == vm.CategoryId && c.IsActive);
        var brand = await _db.Brands.FirstOrDefaultAsync(b => b.Name == vm.Brand && b.IsActive);
        if (category == null) ModelState.AddModelError(nameof(vm.CategoryId), "Danh mục không hợp lệ hoặc đã bị ẩn.");
        if (brand == null) ModelState.AddModelError(nameof(vm.Brand), "Thương hiệu không hợp lệ hoặc đã bị ẩn.");
        var imageUrl = CleanImageUrl(vm.ImageUrl);
        if (vm.ImageFile is { Length: > 0 } upload)
        {
            var allowed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["image/jpeg"] = ".jpg", ["image/png"] = ".png", ["image/webp"] = ".webp", ["image/gif"] = ".gif"
            };
            if (upload.Length > 5 * 1024 * 1024)
                ModelState.AddModelError(nameof(vm.ImageFile), "Ảnh tối đa 5 MB.");
            else if (!allowed.TryGetValue(upload.ContentType ?? "", out _))
                ModelState.AddModelError(nameof(vm.ImageFile), "Chỉ nhận ảnh JPG, PNG, WEBP hoặc GIF.");
            else imageUrl = null; // store only after all other validation succeeds
        }
        else if (!string.IsNullOrWhiteSpace(vm.ImageUrl) && imageUrl == null)
            ModelState.AddModelError(nameof(vm.ImageUrl), "Ảnh cần là đường dẫn nội bộ /images/... hoặc ảnh đã tải lên, hay URL HTTPS hợp lệ.");
        if (!ModelState.IsValid) return View("Create", vm);
        if (vm.ImageFile is { Length: > 0 } validUpload)
            imageUrl = await SaveImageAsync(validUpload);

        Product product;
        if (vm.Id > 0)
        {
            product = await _db.Products.Include(p => p.Images).Include(p => p.Specs)
                .FirstOrDefaultAsync(p => p.Id == vm.Id && p.IsMarketplaceListing && p.OwnerUserId == user.Id)
                ?? null!;
            if (product == null) return NotFound();
            if (product.ListingStatus == ListingStatus.Approved)
            { TempData["Error"] = "Tin đã được duyệt nên không thể sửa trực tiếp."; return RedirectToAction(nameof(Index)); }
            _db.ProductImages.RemoveRange(product.Images);
            product.Images.Clear();
            _db.ProductSpecs.RemoveRange(product.Specs);
            product.Specs.Clear();
        }
        else
        {
            product = new Product { OwnerUserId = user.Id, IsMarketplaceListing = true, CreatedAt = DateTime.Now };
            _db.Products.Add(product);
        }

        product.Name = vm.Name.Trim();
        product.Slug = string.IsNullOrWhiteSpace(product.Slug) || vm.Id == 0 ? Slugify(product.Name) : product.Slug;
        product.ShortDescription = vm.Description.Trim().Length > 160 ? vm.Description.Trim()[..160] : vm.Description.Trim();
        product.Description = vm.Description.Trim();
        product.CategoryId = category!.Id;
        product.BrandId = brand!.Id;
        product.OriginalPrice = vm.Price;
        product.SalePrice = null;
        product.Stock = vm.Stock;
        product.IsFeatured = false;
        product.IsActive = false;
        product.IsMarketplaceListing = true;
        product.ListingStatus = ListingStatus.Pending;
        product.OwnerUserId = user.Id;
        product.PlatformFeeRate = (await _settings.GetSiteAsync()).PlatformFeeRate;
        product.RejectReason = null;
        product.UpdatedAt = DateTime.Now;
        product.Tags = "marketplace";
        product.Images.Add(new ProductImage
        {
            Url = imageUrl ?? "/images/products/fallback.svg",
            Alt = $"Ảnh minh họa: {product.Name}", IsPrimary = true, SortOrder = 0
        });
        product.Specs.Add(new ProductSpec { Name = "Nguồn đăng", Value = "Marketplace", SortOrder = 0 });
        product.Specs.Add(new ProductSpec { Name = "Trạng thái kiểm duyệt", Value = "Chờ cửa hàng xác minh", SortOrder = 1 });

        await _db.SaveChangesAsync();
        await _audit.LogAsync(user.Email, vm.Id > 0 ? "Gửi lại listing Marketplace" : "Tạo listing Marketplace", "Product", product.Id.ToString(), product.Name);
        TempData["Success"] = "Đã gửi sản phẩm để quản trị viên kiểm duyệt. Sản phẩm chưa hiển thị công khai cho đến khi được duyệt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Route("kenh-ban-hang/{id:int}/rut-tin")]
    public async Task<IActionResult> Withdraw(int id)
    {
        var user = await _auth.GetCurrentAsync();
        if (user == null) return Challenge();
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && p.IsMarketplaceListing && p.OwnerUserId == user.Id);
        if (product == null) return NotFound();
        if (product.ListingStatus == ListingStatus.Approved)
        {
            TempData["Error"] = "Tin đã được duyệt. Hãy liên hệ quản trị viên để tạm ẩn tin.";
            return RedirectToAction(nameof(Index));
        }
        product.IsActive = false;
        product.ListingStatus = ListingStatus.Rejected;
        product.RejectReason = "Người bán đã rút tin.";
        product.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(user.Email, "Rút listing Marketplace", "Product", product.Id.ToString(), product.Name);
        TempData["Success"] = "Đã rút tin đăng.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadOptionsAsync(SellerCreateVM vm)
    {
        vm.Categories = await _db.Categories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync();
        vm.Brands = await _db.Brands.AsNoTracking().Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
    }

    private async Task<string> SaveImageAsync(Microsoft.AspNetCore.Http.IFormFile upload)
    {
        var extension = (upload.ContentType ?? "").ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg", "image/png" => ".png", "image/webp" => ".webp", "image/gif" => ".gif", _ => ".bin"
        };
        var folder = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "marketplace");
        Directory.CreateDirectory(folder);
        var fileName = Guid.NewGuid().ToString("N") + extension;
        var fullPath = Path.Combine(folder, fileName);
        await using (var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            await upload.CopyToAsync(stream);
        return "/uploads/marketplace/" + fileName;
    }

    private static string? CleanImageUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var input = value.Trim();
        if ((input.StartsWith("/images/", StringComparison.OrdinalIgnoreCase) || input.StartsWith("/uploads/marketplace/", StringComparison.OrdinalIgnoreCase)) && !input.Contains("..") && !input.Contains('\\')) return input;
        return Uri.TryCreate(input, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps ? uri.AbsoluteUri : null;
    }

    private static string Slugify(string text)
    {
        var normalized = text.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
        var builder = new System.Text.StringBuilder();
        foreach (var ch in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
            builder.Append(char.IsLetterOrDigit(ch) ? ch : '-');
        }
        var slug = System.Text.RegularExpressions.Regex.Replace(builder.ToString(), "-+", "-").Trim('-');
        var suffix = Guid.NewGuid().ToString("N")[..4];
        return string.IsNullOrWhiteSpace(slug) ? $"listing-{suffix}" : $"{slug}-{suffix}";
    }
}
