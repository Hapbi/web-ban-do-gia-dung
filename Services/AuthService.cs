using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using NovaShop.Data;
using NovaShop.Models;

namespace NovaShop.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;
    public AuthService(AppDbContext db, IHttpContextAccessor http) { _db = db; _http = http; }

    public ClaimsPrincipal BuildPrincipal(User u)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new(ClaimTypes.Name, u.FullName),
            new(ClaimTypes.Email, u.Email),
            new(ClaimTypes.Role, u.Role),
            new("CanVerifyPayments", u.CanVerifyPayments ? "true" : "false")
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    public async Task<User?> GetCurrentAsync()
    {
        var ctx = _http.HttpContext;
        if (ctx?.User.Identity?.IsAuthenticated != true) return null;
        var idStr = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idStr, out var id)) return null;
        return await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
    }

    public int? GetCurrentId()
    {
        var ctx = _http.HttpContext;
        if (ctx?.User.Identity?.IsAuthenticated != true) return null;
        var idStr = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idStr, out var id) ? id : null;
    }

    public string GetOwnerKey()
    {
        var ctx = _http.HttpContext!;
        if (ctx.User.Identity?.IsAuthenticated == true)
            return "u:" + (ctx.User.FindFirstValue(ClaimTypes.Email) ?? "guest");
        if (!ctx.Session.Keys.Contains("guest_id"))
            ctx.Session.SetString("guest_id", "g:" + Guid.NewGuid().ToString("N")[..8]);
        return ctx.Session.GetString("guest_id")!;
    }
}


