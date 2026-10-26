using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovaShop.Data;
using NovaShop.Models;
using NovaShop.Services;

namespace NovaShop.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly AuthService _auth;
    public AccountController(AppDbContext db, AuthService auth) { _db = db; _auth = auth; }

    [HttpGet, Route("dang-nhap")]
    public IActionResult Login(string? returnUrl = null) => View(new LoginVM { ReturnUrl = returnUrl });

    [HttpPost, Route("dang-nhap")]
    public async Task<IActionResult> Login(LoginVM vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var email = vm.Email.Trim().ToLower();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
        if (user == null || !user.IsActive || !PasswordSecurity.Verify(user.PasswordHash, vm.Password, out var needsUpgrade))
        {
            ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
            return View(vm);
        }
        if (needsUpgrade) user.PasswordHash = PasswordSecurity.HashPassword(vm.Password);
        user.LastLoginAt = DateTime.Now;
        await _db.SaveChangesAsync();

        var principal = _auth.BuildPrincipal(user);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = vm.RememberMe });

        // Merge guest cart
        await MergeGuestCartAsync(user.Email);

        TempData["Success"] = $"Xin chào, {user.FullName}!";
        if (!string.IsNullOrWhiteSpace(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl)) return Redirect(vm.ReturnUrl);
        if (user.Role == "Admin") return Redirect("/admin");
        if (user.Role == "Staff") return Redirect("/staff");
        return RedirectToAction("Index", "Home");
    }

    [HttpPost, Route("dang-xuat")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet, Route("dang-ky")]
    public IActionResult Register() => View(new RegisterVM());

    [HttpPost, Route("dang-ky")]
    public async Task<IActionResult> Register(RegisterVM vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var email = vm.Email.Trim().ToLower();
        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email))
        {
            ModelState.AddModelError("Email", "Email đã tồn tại");
            return View(vm);
        }
        var user = new User
        {
            Email = email,
            PasswordHash = PasswordSecurity.HashPassword(vm.Password),
            FullName = vm.FullName.Trim(),
            Phone = vm.Phone.Trim(),
            Role = "Customer",
            Avatar = vm.FullName.Trim()[..1].ToUpper()
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var principal = _auth.BuildPrincipal(user);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        TempData["Success"] = "Đăng ký thành công!";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet, Route("quen-mat-khau")]
    public IActionResult ForgotPassword() => View(new ForgotPasswordVM());

    [HttpPost, Route("quen-mat-khau")]
    public IActionResult ForgotPassword(ForgotPasswordVM vm)
    {
        TempData["Error"] = "Bản demo BTVH chưa cấu hình dịch vụ gửi email đặt lại mật khẩu. Vui lòng liên hệ quản trị viên để khôi phục tài khoản.";
        return RedirectToAction(nameof(Login));
    }

    [Route("khong-co-quyen")]
    public IActionResult Denied() { Response.StatusCode = 403; return View(); }

    private async Task MergeGuestCartAsync(string userEmail)
    {
        var guestId = HttpContext.Session.GetString("guest_id");
        if (string.IsNullOrEmpty(guestId)) return;
        var guestItems = await _db.CartItems.Where(c => c.OwnerKey == guestId).ToListAsync();
        var userKey = "u:" + userEmail;
        foreach (var g in guestItems)
        {
            var existing = await _db.CartItems.FirstOrDefaultAsync(c => c.OwnerKey == userKey && c.ProductId == g.ProductId);
            if (existing != null) existing.Quantity += g.Quantity;
            else _db.CartItems.Add(new CartItem { OwnerKey = userKey, ProductId = g.ProductId, Quantity = g.Quantity });
            _db.CartItems.Remove(g);
        }
        await _db.SaveChangesAsync();
        HttpContext.Session.Remove("guest_id");
    }
}


