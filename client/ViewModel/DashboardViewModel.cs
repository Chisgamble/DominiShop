using DominiShop.Model;
using DominiShop.Repository;
using DominiShop.Service;
using Microsoft.UI.Dispatching;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace DominiShop.ViewModel
{
    public partial class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly DashboardService _service;
        private readonly AuthService _authService;

        public event PropertyChangedEventHandler? PropertyChanged;

        // Summary stats
        private int _totalProducts;
        public int TotalProducts
        {
            get => _totalProducts;
            set { _totalProducts = value; OnPropertyChanged(); }
        }

        private int _todayOrderCount;
        public int TodayOrderCount
        {
            get => _todayOrderCount;
            set { _todayOrderCount = value; OnPropertyChanged(); }
        }

        private decimal _todayRevenue;
        public decimal TodayRevenue
        {
            get => _todayRevenue;
            set { _todayRevenue = value; OnPropertyChanged(); }
        }

        private string _topProductName = "—";
        public string TopProductName
        {
            get => _topProductName;
            set { _topProductName = value; OnPropertyChanged(); }
        }

        private decimal _topProductTodayRevenue;
        public decimal TopProductTodayRevenue
        {
            get => _topProductTodayRevenue;
            set { _topProductTodayRevenue = value; OnPropertyChanged(); }
        }

        // Lists
        public ObservableCollection<Product> LowStockProducts { get; } = new();
        public ObservableCollection<Product> BestSellingProducts { get; } = new();
        public ObservableCollection<DailyRevenue> DailyRevenueData { get; } = new();

        // States
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        // Constructor
        public DashboardViewModel(DashboardService service, AuthService authService)
        {
            _service = service;
            _authService = authService;
        }

        // Load all dashboard data
        public async Task LoadAsync()
        {
            var ownerId = _authService.CurrentOwnerId;
            if (ownerId == null)
            {
                ErrorMessage = "Owner not logged in.";
                return;
            }

            IsLoading = true;
            ErrorMessage = null;

            try
            {
                var totalProductsTask = _service.GetTotalProductsAsync(ownerId.Value);
                var lowStockTask = _service.GetLowStockProductsAsync(ownerId.Value);
                var bestSellingTask = _service.GetBestSellingProductsAsync(ownerId.Value);
                var todayOrderCountTask = _service.GetTodayOrderCountAsync(ownerId.Value);
                var todayRevenueTask = _service.GetTodayRevenueAsync(ownerId.Value);
                var dailyRevenueTask = _service.GetDailyRevenueAsync(ownerId.Value, days: 30);
                var topProductTask = _service.GetTopProductWithTodayRevenueAsync(ownerId.Value);

                await Task.WhenAll(
                    totalProductsTask,
                    lowStockTask,
                    bestSellingTask,
                    todayOrderCountTask,
                    todayRevenueTask,
                    dailyRevenueTask,
                    topProductTask);

                TotalProducts = totalProductsTask.Result;
                TodayOrderCount = todayOrderCountTask.Result;
                TodayRevenue = todayRevenueTask.Result;

                var topResult = topProductTask.Result;
                TopProductName = topResult?.Product.Name ?? "—";
                TopProductTodayRevenue = topResult?.TodayRevenue ?? 0;

                LowStockProducts.Clear();
                foreach (var p in lowStockTask.Result)
                    LowStockProducts.Add(p);

                BestSellingProducts.Clear();
                foreach (var p in bestSellingTask.Result)
                    BestSellingProducts.Add(p);

                DailyRevenueData.Clear();
                foreach (var d in dailyRevenueTask.Result)
                    DailyRevenueData.Add(d);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DashboardViewModel.LoadAsync failed");
                ErrorMessage = $"Failed to load dashboard: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}