using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaShop.Services;

namespace NovaShop.Controllers;

[Authorize]
public class WishlistController : Controller
{
    private readonly WishlistService _wishlist;
    public WishlistController(WishlistService wishlist) => _wishlist = wishlist;

    [Route("yeu-thich")]
    public async Task<IActionResult> Index() => View(await _wishlist.GetUserWishlistAsync());

    [HttpPost, Route("yeu-thich/toggle")]
    public async Task<IActionResult> Toggle(int productId, string? returnUrl = null)
    {
        var (ok, msg) = await _wishlist.ToggleAsync(productId);
        TempData[ok ? "Success" : "Error"] = msg;
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { ok, msg });
        if (returnUrl != null && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }
}


