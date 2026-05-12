using DominiShop.DataAccess;
using DominiShop.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DominiShop.Repository
{
    public class DashboardRepository(PostgresContext context)
    {
        private readonly PostgresContext _context = context;

        // Total number of active products
        public async Task<int> GetTotalProductsAsync(int ownerId)
        {
            return await _context.Products
                .Where(p => p.OwnerId == ownerId && p.IsDeleted != true)
                .CountAsync();
        }

        // Top 5 products with quantity less than 5 (low stock), sorted ascending.
        public async Task<List<Product>> GetLowStockProductsAsync(int ownerId, int threshold = 5, int top = 5)
        {
            return await _context.Products
                .Where(p => p.OwnerId == ownerId && p.IsDeleted != true && p.Quantity < threshold)
                .OrderBy(p => p.Quantity)
                .Take(top)
                .ToListAsync();
        }

        // Top 5 best-selling products, sorted by Sold descending.
        public async Task<List<Product>> GetBestSellingProductsAsync(int ownerId, int top = 5)
        {
            return await _context.Products
                .Where(p => p.OwnerId == ownerId && p.IsDeleted != true)
                .OrderByDescending(p => p.Sold)
                .Take(top)
                .ToListAsync();
        }

        // Total number of orders placed today.
        public async Task<int> GetTodayOrderCountAsync(int ownerId)
        {
            var today = DateTime.UtcNow.Date;
            return await _context.Orders
                .Where(o => o.OwnerId == ownerId && o.OrderAt.Date == today)
                .CountAsync();
        }

        // Total revenue (sum of TotalPrice) for today.
        public async Task<decimal> GetTodayRevenueAsync(int ownerId)
        {
            var today = DateTime.UtcNow.Date;
            return await _context.Orders
                .Where(o => o.OwnerId == ownerId && o.OrderAt.Date == today)
                .SumAsync(o => (decimal?)o.TotalPrice ?? 0);
        }

        // Daily revenue for the last days (including today),
        // returned as a list of (Date, Revenue) ordered ascending by date.
        public async Task<List<DailyRevenue>> GetDailyRevenueAsync(int ownerId, int days = 30)
        {
            var from = DateTime.UtcNow.Date.AddDays(-(days - 1));

            var raw = await _context.Orders
                .Where(o => o.OwnerId == ownerId && o.OrderAt >= from)
                .GroupBy(o => o.OrderAt.Date)
                .Select(g => new DailyRevenue
                {
                    Date = g.Key,
                    Revenue = g.Sum(o => (decimal?)o.TotalPrice ?? 0)
                })
                .ToListAsync();

            // Fill in days with zero revenue so the chart has no gaps
            var result = new List<DailyRevenue>();
            for (int i = 0; i < days; i++)
            {
                var date = from.AddDays(i);
                var existing = raw.FirstOrDefault(r => r.Date == date);
                result.Add(existing ?? new DailyRevenue { Date = date, Revenue = 0 });
            }

            return result;
        }

        // Returns the #1 best-selling product (by all-time Sold) and the total revenue that product generated today.
        // Returns null if there are no products.
        public async Task<(Product Product, decimal TodayRevenue)?> GetTopProductWithTodayRevenueAsync(int ownerId)
        {
            var top = await _context.Products
                .Where(p => p.OwnerId == ownerId && p.IsDeleted != true)
                .OrderByDescending(p => p.Sold)
                .FirstOrDefaultAsync();

            if (top == null) return null;

            var today = DateTime.UtcNow.Date;

            var todayRevenue = await _context.OrderDetails
                .Where(od => od.ProductId == top.Id
                          && od.Order.OwnerId == ownerId
                          && od.Order.OrderAt.Date == today)
                .SumAsync(od => (decimal?)(od.Price * od.Quantity) ?? 0);

            return (top, todayRevenue);
        }
    }

    public class DailyRevenue
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
    }
}