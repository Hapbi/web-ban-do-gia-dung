using Microsoft.EntityFrameworkCore;
using NovaShop.Data;
using NovaShop.Models;

namespace NovaShop.Services;

public class NotificationService
{
    private readonly AppDbContext _db;
    public NotificationService(AppDbContext db) => _db = db;

    public async Task CreateAsync(int userId, string title, string message, string link = "", string icon = "🔔")
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId, Title = title, Message = message, Link = link, Icon = icon
        });
        await _db.SaveChangesAsync();
    }

    public async Task<List<Notification>> GetRecentAsync(int userId, int count = 10)
    {
        return await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(count).ToListAsync();
    }

    public async Task<int> UnreadCountAsync(int userId)
    {
        return await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task MarkAllReadAsync(int userId)
    {
        var unread = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
        foreach (var n in unread) n.IsRead = true;
        await _db.SaveChangesAsync();
    }
}


