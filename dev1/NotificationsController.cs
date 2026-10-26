using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaShop.Services;

namespace NovaShop.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly NotificationService _notif;
    private readonly AuthService _auth;
    public NotificationsController(NotificationService notif, AuthService auth) { _notif = notif; _auth = auth; }

    [HttpGet, Route("thong-bao")]
    public async Task<IActionResult> Index()
    {
        var uid = _auth.GetCurrentId();
        if (uid == null) return Unauthorized();
        var list = await _notif.GetRecentAsync(uid.Value, 50);
        await _notif.MarkAllReadAsync(uid.Value);
        return View(list);
    }

    [HttpPost, Route("thong-bao/danh-dau")]
    public async Task<IActionResult> MarkRead()
    {
        var uid = _auth.GetCurrentId();
        if (uid != null) await _notif.MarkAllReadAsync(uid.Value);
        return Ok();
    }
}


