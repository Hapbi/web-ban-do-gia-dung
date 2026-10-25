using Microsoft.EntityFrameworkCore;
using NovaShop.Data;
using NovaShop.Models;

namespace NovaShop.Services;

public class ReportService
{
    private readonly AppDbContext _db;
    public ReportService(AppDbContext db) => _db = db;

    public async Task<DashboardVM> GetDashboardAsync()
    {
        var now = DateTime.Now;
        var today = now.Date;
        var yesterday = today.AddDays(-1);
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var orders = await _db.Orders.Include(o => o.Items).ToListAsync();
        var paid = orders.Where(o => o.Payment == PaymentStatus.Paid).ToList();

        var vm = new DashboardVM
        {
            RevenueToday = paid.Where(o => o.CreatedAt >= today).Sum(o => o.Total),
            RevenueYesterday = paid.Where(o => o.CreatedAt >= yesterday && o.CreatedAt < today).Sum(o => o.Total),
            RevenueMonth = paid.Where(o => o.CreatedAt >= monthStart).Sum(o => o.Total),
            RevenueTotal = paid.Sum(o => o.Total),
            PaidOrders = paid.Count,
            PendingOrders = orders.Count(o => o.Status == OrderStatus.Pending),
            TotalOrders = orders.Count,
            TotalUsers = await _db.Users.CountAsync(u => u.Role == "Customer"),
            TotalProducts = await _db.Products.CountAsync(),
            PendingPayments = await _db.Orders.CountAsync(o => o.PaymentMethod == PaymentMethod.BankTransfer && o.Payment == PaymentStatus.Unpaid && o.Status != OrderStatus.Cancelled),
            PendingListings = await _db.Products.CountAsync(p => p.IsMarketplaceListing && p.ListingStatus == ListingStatus.Pending),
            NewContacts = await _db.ContactMessages.CountAsync(c => c.Status == ContactStatus.New),
            RecentOrders = orders.OrderByDescending(o => o.CreatedAt).Take(10).ToList(),
            StatusCounts = orders.GroupBy(o => o.Status).ToDictionary(g => g.Key, g => g.Count())
        };

        for (int i = 6; i >= 0; i--)
        {
            var day = today.AddDays(-i);
            var next = day.AddDays(1);
            var dayOrders = paid.Where(o => o.CreatedAt >= day && o.CreatedAt < next).ToList();
            vm.Chart7Days.Add((day.ToString("dd/MM"), dayOrders.Sum(o => o.Total), dayOrders.Count));
        }
        vm.ChartMax = Math.Max(vm.Chart7Days.Max(c => c.Amount), 1);

        for (int i = 11; i >= 0; i--)
        {
            var m = now.AddMonths(-i);
            var start = new DateTime(m.Year, m.Month, 1);
            var end = start.AddMonths(1);
            var mOrders = paid.Where(o => o.CreatedAt >= start && o.CreatedAt < end).ToList();
            vm.Chart12Months.Add((start.ToString("MM/yy"), mOrders.Sum(o => o.Total), mOrders.Count));
        }

        var products = await _db.Products.ToListAsync();
        vm.InStock = products.Count(p => p.Stock > 5);
        vm.LowStock = products.Count(p => p.Stock > 0 && p.Stock <= 5);
        vm.OutStock = products.Count(p => p.Stock <= 0);
        vm.LowStockProducts = products.Where(p => p.Stock > 0 && p.Stock <= 5).OrderBy(p => p.Stock).Take(5).ToList();

        vm.TopProducts = paid.SelectMany(o => o.Items)
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(i => i.Quantity), Revenue = g.Sum(i => i.UnitPrice * i.Quantity) })
            .OrderByDescending(x => x.Revenue).Take(5)
            .Select(x => (products.FirstOrDefault(p => p.Id == x.ProductId) ?? new Product { Name = "N/A" }, x.Qty, x.Revenue))
            .ToList();

        vm.TopCustomers = paid.GroupBy(o => o.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count(), Spent = g.Sum(o => o.Total) })
            .OrderByDescending(x => x.Spent).Take(5)
            .Select(x => (_db.Users.FirstOrDefault(u => u.Id == x.UserId) ?? new User { FullName = "N/A" }, x.Count, x.Spent))
            .ToList();

        vm.RecentReviews = await _db.Reviews.Include(r => r.User).Include(r => r.Product).OrderByDescending(r => r.CreatedAt).Take(5).ToListAsync();
        return vm;
    }

    public async Task<OperationsReportVM> GetOperationsAsync()
    {
        var now = DateTime.Now;
        var today = now.Date;
        var weekAgo = now.AddDays(-7);
        var orders = await _db.Orders.Include(o => o.Items).ToListAsync();

        var vm = new OperationsReportVM
        {
            Date = today,
            OrdersToday = orders.Count(o => o.CreatedAt >= today),
            OrdersWeek = orders.Count(o => o.CreatedAt >= weekAgo),
            PendingPayments = orders.Count(o => o.PaymentMethod == PaymentMethod.BankTransfer && o.Payment == PaymentStatus.Unpaid && o.Status != OrderStatus.Cancelled),
            PendingOrders = orders.Count(o => o.Status == OrderStatus.Pending),
            ShippingOrders = orders.Count(o => o.Status == OrderStatus.Shipping),
            OldPendingOrders = orders.Count(o => o.Status == OrderStatus.Pending && o.CreatedAt < now.AddHours(-24)),
            NewContacts = await _db.ContactMessages.CountAsync(c => c.Status == ContactStatus.New),
            NewListings = await _db.Products.CountAsync(p => p.IsMarketplaceListing && p.ListingStatus == ListingStatus.Pending),
            StatusCounts = orders.GroupBy(o => o.Status).ToDictionary(g => g.Key, g => g.Count()),
            PendingPaymentOrders = orders.Where(o => o.PaymentMethod == PaymentMethod.BankTransfer && o.Payment == PaymentStatus.Unpaid && o.Status != OrderStatus.Cancelled)
                .OrderBy(o => o.CreatedAt).Take(10).ToList(),
            OldPendingList = orders.Where(o => o.Status == OrderStatus.Pending && o.CreatedAt < now.AddHours(-24))
                .OrderBy(o => o.CreatedAt).Take(10).ToList()
        };

        var products = await _db.Products.ToListAsync();
        vm.LowStockProducts = products.Count(p => p.Stock > 0 && p.Stock <= 5);
        vm.OutStockProducts = products.Count(p => p.Stock <= 0);
        vm.LowStockList = products.Where(p => p.Stock > 0 && p.Stock <= 5).OrderBy(p => p.Stock).Take(10).ToList();
        vm.OutStockList = products.Where(p => p.Stock <= 0).Take(10).ToList();
        return vm;
    }

    public async Task<RevenueReportVM> GetRevenueAsync(DateTime from, DateTime to)
    {
        var start = from.Date;
        var end = to.Date.AddDays(1);

        var orders = await _db.Orders.Include(o => o.Items)
            .Where(o => o.CreatedAt >= start && o.CreatedAt < end).ToListAsync();
        var paid = orders.Where(o => o.Payment == PaymentStatus.Paid).ToList();

        var vm = new RevenueReportVM
        {
            From = start, To = to.Date,
            TotalRevenue = paid.Sum(o => o.Total),
            TotalOrders = orders.Count,
            PaidOrders = paid.Count,
            CancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled)
        };

        var paidItems = paid.SelectMany(o => o.Items).ToList();
        var products = await _db.Products.Include(p => p.Category).ToListAsync();

        vm.PlatformFees = paidItems.Sum(i =>
        {
            var product = products.FirstOrDefault(p => p.Id == i.ProductId);
            return product?.IsMarketplaceListing == true ? i.LineTotal * product.PlatformFeeRate : 0m;
        });

        for (var d = start; d < end; d = d.AddDays(1))
        {
            var dayOrders = paid.Where(o => o.CreatedAt.Date == d).ToList();
            vm.Daily.Add((d, dayOrders.Count, dayOrders.Sum(o => o.Total)));
        }
        vm.MaxDailyRevenue = vm.Daily.Any() ? vm.Daily.Max(x => x.Revenue) : 1;

        vm.TopProducts = paidItems.GroupBy(i => i.ProductName)
            .Select(g => (g.Key, g.Sum(i => i.Quantity), g.Sum(i => i.LineTotal)))
            .OrderByDescending(x => x.Item3).Take(10).ToList();

        vm.ByCategory = paidItems.GroupBy(i => products.FirstOrDefault(p => p.Id == i.ProductId)?.Category?.Name ?? "Khác")
            .Select(g => (g.Key, g.Sum(i => i.LineTotal)))
            .OrderByDescending(x => x.Item2).ToList();

        return vm;
    }
}
