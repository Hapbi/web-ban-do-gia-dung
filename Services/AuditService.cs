using NovaShop.Data;
using NovaShop.Models;

namespace NovaShop.Services;

public class AuditService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;
    public AuditService(AppDbContext db, IHttpContextAccessor http) { _db = db; _http = http; }

    public async Task LogAsync(string? userEmail, string action, string entity, string entityId, string details = "")
    {
        try
        {
            _db.AuditLogs.Add(new AuditLog
            {
                UserEmail = string.IsNullOrWhiteSpace(userEmail) ? "system" : userEmail,
                Action = action, Entity = entity, EntityId = entityId, Details = details,
                IpAddress = _http.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? ""
            });
            await _db.SaveChangesAsync();
        }
        catch
        {
            // A logging failure should not undo the primary business operation.
        }
    }
}
