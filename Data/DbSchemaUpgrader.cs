using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace NovaShop.Data;

/// <summary>
/// Small additive SQLite upgrader for the demo app. It lets an existing local btvh.db
/// receive the new admin/staff columns and tables without making the user delete their data.
/// For a production deployment, use versioned EF Core migrations instead.
/// </summary>
public static class DbSchemaUpgrader
{
    public static async Task EnsureAsync(AppDbContext db)
    {
        await db.Database.OpenConnectionAsync();
        try
        {
            var conn = db.Database.GetDbConnection();
            await EnsureColumn(conn, "Users", "CanVerifyPayments", "INTEGER NOT NULL DEFAULT 0");
            await EnsureColumn(conn, "Addresses", "CreatedAt", "TEXT NOT NULL DEFAULT '2000-01-01 00:00:00'");
            await EnsureColumn(conn, "Brands", "Logo", "TEXT NOT NULL DEFAULT ''");
            await EnsureColumn(conn, "ProductImages", "Alt", "TEXT NOT NULL DEFAULT ''");
            await EnsureColumn(conn, "Reviews", "ImageUrl", "TEXT NOT NULL DEFAULT ''");
            await EnsureColumn(conn, "BlogPosts", "Tags", "TEXT NOT NULL DEFAULT ''");
            await EnsureColumn(conn, "Products", "IsMarketplaceListing", "INTEGER NOT NULL DEFAULT 0");
            await EnsureColumn(conn, "Products", "ListingStatus", "INTEGER NOT NULL DEFAULT 0");
            await EnsureColumn(conn, "Products", "OwnerUserId", "INTEGER NULL");
            await EnsureColumn(conn, "Products", "PlatformFeeRate", "TEXT NOT NULL DEFAULT '0.07'");
            await EnsureColumn(conn, "Products", "RejectReason", "TEXT NULL");
            await EnsureColumn(conn, "Orders", "PaymentProofUrl", "TEXT NULL");
            await EnsureColumn(conn, "Orders", "PaymentNote", "TEXT NULL");
            await EnsureColumn(conn, "ContactMessages", "Type", "INTEGER NOT NULL DEFAULT 0");
            await EnsureColumn(conn, "ContactMessages", "Status", "INTEGER NOT NULL DEFAULT 0");
            await EnsureColumn(conn, "ContactMessages", "StaffNote", "TEXT NULL");
            await EnsureColumn(conn, "ContactMessages", "HandledBy", "TEXT NULL");
            await EnsureColumn(conn, "ContactMessages", "ResolvedAt", "TEXT NULL");
            await EnsureColumn(conn, "AuditLogs", "IpAddress", "TEXT NOT NULL DEFAULT ''");

            await EnsureTable(conn, "Tags", "CREATE TABLE IF NOT EXISTS \"Tags\" (\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_Tags\" PRIMARY KEY AUTOINCREMENT, \"Name\" TEXT NOT NULL, \"Slug\" TEXT NOT NULL);");
            await EnsureTable(conn, "Newsletters", "CREATE TABLE IF NOT EXISTS \"Newsletters\" (\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_Newsletters\" PRIMARY KEY AUTOINCREMENT, \"Email\" TEXT NOT NULL, \"IsActive\" INTEGER NOT NULL DEFAULT 1, \"SubscribedAt\" TEXT NOT NULL DEFAULT '2000-01-01 00:00:00');");
            await EnsureColumn(conn, "Newsletters", "IsActive", "INTEGER NOT NULL DEFAULT 1");
            await EnsureColumn(conn, "Newsletters", "SubscribedAt", "TEXT NOT NULL DEFAULT '2000-01-01 00:00:00'");
            await EnsureTable(conn, "Banners", "CREATE TABLE IF NOT EXISTS \"Banners\" (\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_Banners\" PRIMARY KEY AUTOINCREMENT, \"Title\" TEXT NOT NULL, \"ImageUrl\" TEXT NOT NULL, \"LinkUrl\" TEXT NULL, \"Position\" TEXT NOT NULL, \"SortOrder\" INTEGER NOT NULL, \"IsActive\" INTEGER NOT NULL, \"CreatedAt\" TEXT NOT NULL);");
            await EnsureTable(conn, "SiteSettings", "CREATE TABLE IF NOT EXISTS \"SiteSettings\" (\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_SiteSettings\" PRIMARY KEY AUTOINCREMENT, \"SiteName\" TEXT NOT NULL, \"Tagline\" TEXT NOT NULL, \"LogoUrl\" TEXT NOT NULL, \"Address\" TEXT NOT NULL, \"Phone\" TEXT NOT NULL, \"Email\" TEXT NOT NULL, \"OpenHours\" TEXT NOT NULL, \"FacebookUrl\" TEXT NOT NULL, \"YoutubeUrl\" TEXT NOT NULL, \"ZaloUrl\" TEXT NOT NULL, \"FreeShipThreshold\" TEXT NOT NULL, \"ShippingFee\" TEXT NOT NULL, \"PlatformFeeRate\" TEXT NOT NULL, \"UpdatedAt\" TEXT NOT NULL);");
            await EnsureTable(conn, "BankSettings", "CREATE TABLE IF NOT EXISTS \"BankSettings\" (\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_BankSettings\" PRIMARY KEY AUTOINCREMENT, \"BankName\" TEXT NOT NULL, \"AccountNumber\" TEXT NOT NULL, \"AccountHolder\" TEXT NOT NULL, \"Branch\" TEXT NOT NULL, \"QrImageUrl\" TEXT NOT NULL, \"TransferTemplate\" TEXT NOT NULL, \"Instructions\" TEXT NOT NULL, \"UpdatedAt\" TEXT NOT NULL);");
            // Older builds may already have the new tables but not every newer setting column.
            foreach (var col in new[] { ("SiteSettings", "SiteName", "TEXT NOT NULL DEFAULT 'BTVH'"), ("SiteSettings", "Tagline", "TEXT NOT NULL DEFAULT ''"), ("SiteSettings", "LogoUrl", "TEXT NOT NULL DEFAULT ''"), ("SiteSettings", "Address", "TEXT NOT NULL DEFAULT ''"), ("SiteSettings", "Phone", "TEXT NOT NULL DEFAULT ''"), ("SiteSettings", "Email", "TEXT NOT NULL DEFAULT ''"), ("SiteSettings", "OpenHours", "TEXT NOT NULL DEFAULT ''"), ("SiteSettings", "FacebookUrl", "TEXT NOT NULL DEFAULT ''"), ("SiteSettings", "YoutubeUrl", "TEXT NOT NULL DEFAULT ''"), ("SiteSettings", "ZaloUrl", "TEXT NOT NULL DEFAULT ''"), ("SiteSettings", "FreeShipThreshold", "TEXT NOT NULL DEFAULT '5000000'"), ("SiteSettings", "ShippingFee", "TEXT NOT NULL DEFAULT '30000'"), ("SiteSettings", "PlatformFeeRate", "TEXT NOT NULL DEFAULT '0.07'"), ("SiteSettings", "UpdatedAt", "TEXT NOT NULL DEFAULT '2000-01-01 00:00:00'"), ("BankSettings", "BankName", "TEXT NOT NULL DEFAULT 'Vietcombank'"), ("BankSettings", "AccountNumber", "TEXT NOT NULL DEFAULT ''"), ("BankSettings", "AccountHolder", "TEXT NOT NULL DEFAULT ''"), ("BankSettings", "Branch", "TEXT NOT NULL DEFAULT ''"), ("BankSettings", "QrImageUrl", "TEXT NOT NULL DEFAULT ''"), ("BankSettings", "TransferTemplate", "TEXT NOT NULL DEFAULT '{OrderCode} - {CustomerName}'"), ("BankSettings", "Instructions", "TEXT NOT NULL DEFAULT ''"), ("BankSettings", "UpdatedAt", "TEXT NOT NULL DEFAULT '2000-01-01 00:00:00'"), ("Banners", "Title", "TEXT NOT NULL DEFAULT ''"), ("Banners", "ImageUrl", "TEXT NOT NULL DEFAULT ''"), ("Banners", "LinkUrl", "TEXT NULL"), ("Banners", "Position", "TEXT NOT NULL DEFAULT 'home-top'"), ("Banners", "SortOrder", "INTEGER NOT NULL DEFAULT 0"), ("Banners", "IsActive", "INTEGER NOT NULL DEFAULT 1"), ("Banners", "CreatedAt", "TEXT NOT NULL DEFAULT '2000-01-01 00:00:00'"), ("Tags", "Name", "TEXT NOT NULL DEFAULT ''"), ("Tags", "Slug", "TEXT NOT NULL DEFAULT ''") })
                await EnsureColumn(conn, col.Item1, col.Item2, col.Item3);
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }

    private static async Task EnsureColumn(DbConnection conn, string table, string column, string definition)
    {
        await using var check = conn.CreateCommand();
        check.CommandText = $"PRAGMA table_info(\"{table}\");";
        await using var reader = await check.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase)) return;
        await reader.CloseAsync();
        await using var alter = conn.CreateCommand();
        alter.CommandText = $"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {definition};";
        await alter.ExecuteNonQueryAsync();
    }

    private static async Task EnsureTable(DbConnection conn, string table, string createSql)
    {
        await using var check = conn.CreateCommand();
        check.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name;";
        var parameter = check.CreateParameter(); parameter.ParameterName = "$name"; parameter.Value = table; check.Parameters.Add(parameter);
        var exists = Convert.ToInt32(await check.ExecuteScalarAsync()) > 0;
        if (!exists)
        {
            await using var create = conn.CreateCommand(); create.CommandText = createSql;
            await create.ExecuteNonQueryAsync();
        }
    }
}
