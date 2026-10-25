using Microsoft.EntityFrameworkCore;
using NovaShop.Data;
using NovaShop.Models;

namespace NovaShop.Services;

public class SettingsService
{
    private readonly AppDbContext _db;
    public SettingsService(AppDbContext db) => _db = db;

    public async Task<SiteSettings> GetSiteAsync() => await _db.SiteSettings.OrderBy(x => x.Id).FirstOrDefaultAsync() ?? new SiteSettings();
    public async Task<BankSettings> GetBankAsync() => await _db.BankSettings.OrderBy(x => x.Id).FirstOrDefaultAsync() ?? new BankSettings();

    public async Task SaveSiteAsync(SiteSettings input)
    {
        var existing = await _db.SiteSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
        if (existing == null) { input.Id = 0; input.UpdatedAt = DateTime.Now; _db.SiteSettings.Add(input); }
        else
        {
            existing.SiteName = input.SiteName.Trim(); existing.Tagline = input.Tagline?.Trim() ?? "";
            existing.LogoUrl = input.LogoUrl?.Trim() ?? ""; existing.Address = input.Address?.Trim() ?? "";
            existing.Phone = input.Phone?.Trim() ?? ""; existing.Email = input.Email?.Trim() ?? "";
            existing.OpenHours = input.OpenHours?.Trim() ?? ""; existing.FacebookUrl = input.FacebookUrl?.Trim() ?? "";
            existing.YoutubeUrl = input.YoutubeUrl?.Trim() ?? ""; existing.ZaloUrl = input.ZaloUrl?.Trim() ?? "";
            existing.FreeShipThreshold = Math.Max(0, input.FreeShipThreshold); existing.ShippingFee = Math.Max(0, input.ShippingFee);
            existing.PlatformFeeRate = Math.Clamp(input.PlatformFeeRate, 0, 1); existing.UpdatedAt = DateTime.Now;
        }
        await _db.SaveChangesAsync();
    }

    public async Task SaveBankAsync(BankSettings input)
    {
        var existing = await _db.BankSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
        if (existing == null) { input.Id = 0; input.UpdatedAt = DateTime.Now; _db.BankSettings.Add(input); }
        else
        {
            existing.BankName = input.BankName.Trim(); existing.AccountNumber = input.AccountNumber.Trim();
            existing.AccountHolder = input.AccountHolder.Trim(); existing.Branch = input.Branch?.Trim() ?? "";
            existing.QrImageUrl = input.QrImageUrl?.Trim() ?? ""; existing.TransferTemplate = input.TransferTemplate?.Trim() ?? "{OrderCode} - {CustomerName}";
            existing.Instructions = input.Instructions?.Trim() ?? ""; existing.UpdatedAt = DateTime.Now;
        }
        await _db.SaveChangesAsync();
    }
}
