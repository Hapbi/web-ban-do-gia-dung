using Microsoft.EntityFrameworkCore;
using NovaShop.Data;
using NovaShop.Models;

namespace NovaShop.Services;

public class RecentlyViewedService
{
    private const string SessionKey = "recently_viewed";
    private readonly IHttpContextAccessor _http;
    private readonly AppDbContext _db;

    public RecentlyViewedService(IHttpContextAccessor http, AppDbContext db) { _http = http; _db = db; }

    public void Record(int productId)
    {
        var ctx = _http.HttpContext;
        if (ctx == null) return;
        var ids = GetIds();
        ids.Remove(productId);
        ids.Insert(0, productId);
        if (ids.Count > 8) ids = ids.Take(8).ToList();
        ctx.Session.SetString(SessionKey, string.Join(",", ids));
    }

    private List<int> GetIds()
    {
        var raw = _http.HttpContext?.Session.GetString(SessionKey);
        if (string.IsNullOrEmpty(raw)) return new List<int>();
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => int.TryParse(x, out var id) ? id : 0).Where(id => id > 0).Distinct().Take(8).ToList();
    }

    public async Task<List<Product>> GetAsync(int excludeId = 0)
    {
        var ids = GetIds().Where(id => id != excludeId).ToList();
        if (ids.Count == 0) return new();
        return await _db.Products
            .Include(p => p.Images).Include(p => p.Reviews).Include(p => p.Brand).Include(p => p.Category)
            .Where(p => p.IsActive && p.Category != null && p.Category.IsActive && p.Brand != null && p.Brand.IsActive
                && (!p.IsMarketplaceListing || p.ListingStatus == ListingStatus.Approved) && ids.Contains(p.Id))
            .ToListAsync();
    }
}


