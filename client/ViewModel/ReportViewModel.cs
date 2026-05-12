using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominiShop.Model;
using DominiShop.Service;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace DominiShop.ViewModel
{
    public partial class ReportViewModel : BaseViewModel
    {
        private readonly ProductService _productService;
        private readonly OrderService _orderService;

        private List<Order> _allOrders = new();
        private List<Product> _allProducts = new();

        [ObservableProperty]
        public partial ObservableCollection<string> TimeFilters { get; set; }

        [ObservableProperty]
        public partial string SelectedFilter { get; set; }

        [ObservableProperty]
        public partial DateTimeOffset? StartDate { get; set; } = DateTimeOffset.Now.AddMonths(-1);

        [ObservableProperty]
        public partial DateTimeOffset? EndDate { get; set; } = DateTimeOffset.Now;

        [ObservableProperty]
        public partial ObservableCollection<Product> AvailableProducts { get; set; } = new();

        [ObservableProperty]
        public partial ObservableCollection<Product> ActiveProducts { get; set; } = new();

        [ObservableProperty]
        public partial ObservableCollection<ISeries> SalesSeries { get; set; } = new();

        [ObservableProperty]
        public partial IEnumerable<ICartesianAxis> SalesXAxes { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<ISeries> RevenueSeries { get; set; } = new();

        [ObservableProperty]
        public partial IEnumerable<ICartesianAxis> RevenueXAxes { get; set; }

        public ReportViewModel(ProductService productService, OrderService orderService)
        {
            _productService = productService;
            _orderService = orderService;

            TimeFilters = new ObservableCollection<string>
            {
                "Custom Date Range", "By Week", "By Month", "By Year"
            };
            SelectedFilter = "By Month";

            SalesXAxes = new[] { new Axis { LabelsRotation = 15 } };
            RevenueXAxes = new[] { new Axis { LabelsRotation = 15 } };
        }

        public async Task InitializeAsync()
        {
            var pTask = _productService.GetProductsAsync();
            var oTask = _orderService.GetOrdersAsync();

            await Task.WhenAll(pTask, oTask);

            var pRes = await pTask;
            if (pRes.Success && pRes.Data != null)
            {
                _allProducts = pRes.Data;
                AvailableProducts = new ObservableCollection<Product>(_allProducts);
            }

            var oRes = await oTask;
            if (oRes.Success && oRes.Data != null)
            {
                _allOrders = oRes.Data;
            }

            GenerateCharts();
        }

        partial void OnSelectedFilterChanged(string value) => GenerateCharts();

        private void GenerateCharts()
        {
            SalesSeries.Clear();

            // 1. Generate Total Sales
            var totalSalesData = GetSalesData(null);
            SalesSeries.Add(new LineSeries<int>
            {
                Values = totalSalesData.Values,
                Name = "Total Sales",
                Fill = null,
                GeometrySize = 8,
                LineSmoothness = 0.5
            });

            // Set X Axis labels based on Total Sales
            SalesXAxes = new[] { new Axis { Labels = totalSalesData.Labels, LabelsRotation = 15 } };

            // 2. Generate Active Products
            foreach (var prod in ActiveProducts)
            {
                var prodSalesData = GetSalesData(prod.Id);
                SalesSeries.Add(new LineSeries<int>
                {
                    Values = prodSalesData.Values,
                    Name = prod.Name,
                    Fill = null,
                    GeometrySize = 8,
                    LineSmoothness = 0.5
                });
            }

            // Generate Revenue/Profit (Placeholder with dynamic labels)
            RevenueSeries.Clear();
            RevenueSeries.Add(new ColumnSeries<double>
            {
                Values = new double[] { 1200.5, 1500, 1400.2, 2600, 3800, 2300, 2900.8 },
                Name = "Revenue ($)",
            });
            RevenueSeries.Add(new ColumnSeries<double>
            {
                Values = new double[] { 350, 450, 400, 800, 1100, 780, 900 },
                Name = "Profit ($)",
            });
            RevenueXAxes = new[] { new Axis { Labels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" }, LabelsRotation = 15 } };
        }

        private (int[] Values, string[] Labels) GetSalesData(int? productId)
        {
            // Simple grouping by day for demonstration. 
            // In a real scenario, this would use SelectedFilter (Week, Month, etc.) and StartDate/EndDate
            var filteredOrders = _allOrders;

            if (filteredOrders.Count == 0)
            {
                return (new int[0], new string[0]);
            }

            var days = filteredOrders.GroupBy(o => o.OrderAt.Date).OrderBy(g => g.Key).ToList();
            
            var values = new List<int>();
            var labels = new List<string>();

            foreach (var day in days)
            {
                labels.Add(day.Key.ToString("MM/dd"));
                int salesCount = 0;

                foreach (var order in day)
                {
                    if (order.OrderDetails != null)
                    {
                        if (productId.HasValue)
                        {
                            salesCount += order.OrderDetails.Where(d => d.ProductId == productId.Value).Sum(d => d.Quantity);
                        }
                        else
                        {
                            salesCount += order.OrderDetails.Sum(d => d.Quantity);
                        }
                    }
                }
                values.Add(salesCount);
            }

            // Fallback if no data
            if (values.Count == 0)
            {
                return (new[] { 0 }, new[] { "No Data" });
            }

            return (values.ToArray(), labels.ToArray());
        }

        [RelayCommand]
        public void AddProductFilter(Product? product)
        {
            if (product != null && !ActiveProducts.Any(p => p.Id == product.Id))
            {
                ActiveProducts.Add(product);
                
                // Regenerate charts to add the new line overlay
                GenerateCharts();
            }
        }

        [RelayCommand]
        public void RemoveProductFilter(Product? product)
        {
            if (product != null)
            {
                var item = ActiveProducts.FirstOrDefault(p => p.Id == product.Id);
                if (item != null)
                {
                    ActiveProducts.Remove(item);
                    GenerateCharts();
                }
            }
        }
    }
}
