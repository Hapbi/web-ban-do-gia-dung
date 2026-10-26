using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovaShop.Data;
using NovaShop.Models;
using NovaShop.Services;

namespace NovaShop.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly AppDbContext _db;
    private readonly AuthService _auth;
    public ProfileController(AppDbContext db, AuthService auth) { _db = db; _auth = auth; }

    [Route("tai-khoan")]
    public async Task<IActionResult> Index()
    {
        var uid = _auth.GetCurrentId();
        if (uid == null) return RedirectToAction("Login", "Account");
        var user = await _db.Users.FindAsync(uid.Value);
        if (user == null) return RedirectToAction("Login", "Account");

        var orders = await _db.Orders.Include(o => o.Items).Where(o => o.UserId == uid.Value).ToListAsync();
        var vm = new ProfileVM
        {
            User = user,
            TotalOrders = orders.Count,
            TotalSpent = orders.Where(o => o.Payment == PaymentStatus.Paid).Sum(o => o.Total),
            WishlistCount = await _db.Wishlists.CountAsync(w => w.UserId == uid.Value),
            ReviewCount = await _db.Reviews.CountAsync(r => r.UserId == uid.Value),
            RecentOrders = orders.OrderByDescending(o => o.CreatedAt).Take(3).ToList()
        };
        return View(vm);
    }

    [HttpGet, Route("tai-khoan/sua")]
    public async Task<IActionResult> Edit()
    {
        var uid = _auth.GetCurrentId();
        var user = uid.HasValue ? await _db.Users.FindAsync(uid.Value) : null;
        if (user == null) return NotFound();
        return View(user);
    }

    [HttpPost, Route("tai-khoan/sua")]
    public async Task<IActionResult> Edit(string fullName, string phone)
    {
        var uid = _auth.GetCurrentId();
        var user = uid.HasValue ? await _db.Users.FindAsync(uid.Value) : null;
        if (user == null) return NotFound();
        user.FullName = fullName?.Trim() ?? user.FullName;
        user.Phone = phone?.Trim() ?? user.Phone;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật thông tin";
        return RedirectToAction(nameof(Index));
    }

    [Route("tai-khoan/dia-chi")]
    public async Task<IActionResult> Addresses()
    {
        var uid = _auth.GetCurrentId();
        var list = await _db.Addresses.Where(a => a.UserId == uid).OrderByDescending(a => a.IsDefault).ToListAsync();
        return View(list);
    }

    [HttpPost, Route("tai-khoan/dia-chi/them")]
    public async Task<IActionResult> AddAddress(string receiverName, string phone, string addressLine, string ward, string district, string city, bool isDefault)
    {
        var uid = _auth.GetCurrentId();
        if (uid == null) return RedirectToAction("Login", "Account");
        if (isDefault)
        {
            var defaults = await _db.Addresses.Where(a => a.UserId == uid.Value && a.IsDefault).ToListAsync();
            foreach (var d in defaults) d.IsDefault = false;
        }
        _db.Addresses.Add(new Address
        {
            UserId = uid.Value, ReceiverName = receiverName, Phone = phone,
            AddressLine = addressLine, Ward = ward, District = district, City = city, IsDefault = isDefault
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã thêm địa chỉ";
        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost, Route("tai-khoan/dia-chi/xoa")]
    public async Task<IActionResult> DeleteAddress(int id)
    {
        var uid = _auth.GetCurrentId();
        var a = await _db.Addresses.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);
        if (a != null) { _db.Addresses.Remove(a); await _db.SaveChangesAsync(); TempData["Success"] = "Đã xóa"; }
        return RedirectToAction(nameof(Addresses));
    }
}


