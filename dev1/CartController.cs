using Microsoft.AspNetCore.Mvc;
using NovaShop.Services;

namespace NovaShop.Controllers;

public class CartController : Controller
{
    private readonly CartService _cart;
    public CartController(CartService cart) => _cart = cart;

    [Route("gio-hang")]
    public async Task<IActionResult> Index(string? coupon) => View(await _cart.GetAsync(coupon));

    [HttpPost, Route("gio-hang/them")]
    public async Task<IActionResult> Add(int productId, int quantity = 1, string? returnUrl = null)
    {
        var (ok, msg) = await _cart.AddAsync(productId, quantity);
        TempData[ok ? "Success" : "Error"] = msg;

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { ok, msg, count = await _cart.GetCountAsync() });

        if (returnUrl != null && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Route("gio-hang/cap-nhat")]
    public async Task<IActionResult> Update(int productId, int quantity)
    {
        await _cart.UpdateAsync(productId, quantity);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Route("gio-hang/xoa")]
    public async Task<IActionResult> Remove(int productId)
    {
        await _cart.RemoveAsync(productId);
        TempData["Success"] = "Đã xóa sản phẩm";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Route("gio-hang/xoa-het")]
    public async Task<IActionResult> Clear()
    {
        await _cart.ClearAsync();
        TempData["Success"] = "Đã xóa toàn bộ giỏ hàng";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Route("gio-hang/ma-giam-gia")]
    public IActionResult ApplyCoupon(string code) => RedirectToAction(nameof(Index), new { coupon = code });
}


