using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using NovaShop.Data;
using NovaShop.Models;
using NovaShop.Services;

namespace NovaShop.Controllers;

public class OrdersController : Controller
{
    private readonly OrderService _orders;
    private readonly AppDbContext _db;
    private readonly SettingsService _settings;
    public OrdersController(OrderService orders, AppDbContext db, SettingsService settings) { _orders = orders; _db = db; _settings = settings; }

    [Authorize, Route("don-hang")]
    public async Task<IActionResult> Index() => View(await _orders.GetMyOrdersAsync());

    [Authorize, Route("don-hang/{code}")]
    public async Task<IActionResult> Detail(string code)
    {
        var o = await _orders.GetByCodeAsync(code);
        if (o == null) return NotFound();
        if (o.PaymentMethod == PaymentMethod.BankTransfer && o.Payment != PaymentStatus.Paid)
        {
            var bank = await _settings.GetBankAsync();
            ViewBag.Bank = bank;
            ViewBag.TransferMemo = (bank.TransferTemplate ?? "{OrderCode} - {CustomerName}")
                .Replace("{OrderCode}", o.Code, StringComparison.OrdinalIgnoreCase)
                .Replace("{CustomerName}", o.CustomerName, StringComparison.OrdinalIgnoreCase);
        }
        return View(o);
    }

    [Authorize, HttpPost, Route("don-hang/{code}/chung-tu")]
    public async Task<IActionResult> SubmitPaymentProof(string code, string proofUrl, string? note)
    {
        var currentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(currentId, out var userId)) return Challenge();
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Code == code && o.UserId == userId);
        if (order == null) return NotFound();
        if (order.PaymentMethod != PaymentMethod.BankTransfer || order.Status == OrderStatus.Cancelled || order.Payment == PaymentStatus.Paid)
        { TempData["Error"] = "Đơn hàng này không nhận bổ sung chứng từ thanh toán"; return RedirectToAction(nameof(Detail), new { code }); }
        if (!Uri.TryCreate(proofUrl, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        { TempData["Error"] = "Hãy nhập đường dẫn chứng từ hợp lệ bắt đầu bằng http:// hoặc https://"; return RedirectToAction(nameof(Detail), new { code }); }
        order.PaymentProofUrl = uri.ToString(); order.PaymentNote = note?.Trim();
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã gửi đường dẫn chứng từ. Nhân viên sẽ kiểm tra giao dịch thực tế.";
        return RedirectToAction(nameof(Detail), new { code });
    }

    [Route("tra-cuu")]
    public IActionResult Track() => View(new TrackOrderVM());

    [HttpPost, Route("tra-cuu")]
    public async Task<IActionResult> Track(TrackOrderVM vm)
    {
        if (!ModelState.IsValid) return View(vm);
        vm.Searched = true;
        vm.Result = await _orders.TrackByCodeAndPhoneAsync(vm.OrderCode, vm.Phone);
        return View(vm);
    }
}


