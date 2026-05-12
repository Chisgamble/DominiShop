using DominiShop.Model;
using DominiShop.Repository;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DominiShop.Service
{
    public class DashboardService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public DashboardService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        private T UseRepository<T>(Func<DashboardRepository, T> func)
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<DashboardRepository>();
            return func(repo);
        }

        private async Task<T> UseRepositoryAsync<T>(Func<DashboardRepository, Task<T>> func)
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<DashboardRepository>();
            return await func(repo);
        }

        public Task<int> GetTotalProductsAsync(int ownerId)
            => UseRepositoryAsync(repo => repo.GetTotalProductsAsync(ownerId));

        public Task<List<Product>> GetLowStockProductsAsync(int ownerId)
            => UseRepositoryAsync(repo => repo.GetLowStockProductsAsync(ownerId));

        public Task<List<Product>> GetBestSellingProductsAsync(int ownerId)
            => UseRepositoryAsync(repo => repo.GetBestSellingProductsAsync(ownerId));

        public Task<int> GetTodayOrderCountAsync(int ownerId)
            => UseRepositoryAsync(repo => repo.GetTodayOrderCountAsync(ownerId));

        public Task<decimal> GetTodayRevenueAsync(int ownerId)
            => UseRepositoryAsync(repo => repo.GetTodayRevenueAsync(ownerId));

        public Task<List<DailyRevenue>> GetDailyRevenueAsync(int ownerId, int days = 30)
            => UseRepositoryAsync(repo => repo.GetDailyRevenueAsync(ownerId, days));
    }
}