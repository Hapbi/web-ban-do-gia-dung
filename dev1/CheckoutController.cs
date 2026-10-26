using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovaShop.Data;
using NovaShop.Models;
using NovaShop.Services;

namespace NovaShop.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly CartService _cart;
    private readonly OrderService _orders;
    private readonly AuthService _auth;
    private readonly AppDbContext _db;
    private readonly SettingsService _settings;

    public CheckoutController(CartService cart, OrderService orders, AuthService auth, AppDbContext db, SettingsService settings)
    { _cart = cart; _orders = orders; _auth = auth; _db = db; _settings = settings; }

    [HttpGet, Route("thanh-toan")]
    public async Task<IActionResult> Index(string? coupon)
    {
        var cart = await _cart.GetAsync(coupon);
        if (cart.IsEmpty) { TempData["Error"] = "Giỏ hàng trống"; return RedirectToAction("Index", "Cart"); }

        ViewBag.BankSettings = await _settings.GetBankAsync();
        var user = await _auth.GetCurrentAsync();
        var vm = new CheckoutVM
        {
            CustomerName = user?.FullName ?? "",
            Phone = user?.Phone ?? "",
            Email = user?.Email ?? "",
            CouponCode = coupon,
            Cart = cart,
            SavedAddresses = user != null ? await _db.Addresses.Where(a => a.UserId == user.Id).ToListAsync() : new()
        };
        var def = vm.SavedAddresses.FirstOrDefault(a => a.IsDefault);
        if (def != null) { vm.Address = def.FullAddress; vm.Phone = def.Phone; vm.CustomerName = def.ReceiverName; }
        return View(vm);
    }

    [HttpPost, Route("thanh-toan")]
    public async Task<IActionResult> Index(CheckoutVM vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Cart = await _cart.GetAsync(vm.CouponCode);
            var u = await _auth.GetCurrentAsync();
            vm.SavedAddresses = u != null ? await _db.Addresses.Where(a => a.UserId == u.Id).ToListAsync() : new();
            ViewBag.BankSettings = await _settings.GetBankAsync();
            return View(vm);
        }

        var (ok, code, err) = await _orders.PlaceOrderAsync(vm);
        if (!ok) { TempData["Error"] = err; return RedirectToAction("Index", "Cart"); }

        TempData["Success"] = "Đặt hàng thành công!";
        return RedirectToAction("Detail", "Orders", new { code });
    }
}


