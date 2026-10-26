using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovaShop.Data;
using NovaShop.Models;

namespace NovaShop.Controllers;

public class ContactController : Controller
{
    private readonly AppDbContext _db;
    public ContactController(AppDbContext db) => _db = db;

    [Route("lien-he")]
    public IActionResult Index() => View(new ContactMessage());

    [HttpPost, Route("lien-he")]
    public async Task<IActionResult> Index(ContactMessage vm)
    {
        if (string.IsNullOrWhiteSpace(vm.FullName) || string.IsNullOrWhiteSpace(vm.Email) || string.IsNullOrWhiteSpace(vm.Message))
        {
            ModelState.AddModelError("", "Vui lòng điền đầy đủ thông tin");
            return View(vm);
        }
        _db.ContactMessages.Add(new ContactMessage
        {
            FullName = vm.FullName.Trim(), Email = vm.Email.Trim(),
            Phone = vm.Phone?.Trim() ?? "", Subject = vm.Subject?.Trim() ?? "",
            Message = vm.Message.Trim()
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Cảm ơn bạn đã liên hệ. Chúng tôi sẽ phản hồi sớm!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Route("dang-ky-nhan-tin")]
    public async Task<IActionResult> Subscribe(string email)
    {
        var normalizedEmail = (email ?? "").Trim().ToLowerInvariant();
        if (normalizedEmail.Length > 254 || !System.Text.RegularExpressions.Regex.IsMatch(normalizedEmail, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
        {
            TempData["Error"] = "Vui lòng nhập địa chỉ email hợp lệ.";
            return RedirectToAction("Index", "Home");
        }
        if (!await _db.Newsletters.AnyAsync(n => n.Email == normalizedEmail))
        {
            _db.Newsletters.Add(new Newsletter { Email = normalizedEmail });
            try { await _db.SaveChangesAsync(); }
            catch (DbUpdateException) { /* A simultaneous signup may have inserted the same unique email. */ }
        }
        TempData["Success"] = "Đã đăng ký nhận tin!";
        return RedirectToAction("Index", "Home");
    }
}


