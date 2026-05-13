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
        public partial bool IsDayVisible { get; set; } = true;

        [ObservableProperty]
        public partial bool IsMonthVisible { get; set; } = true;

        [ObservableProperty]
        public partial ObservableCollection<ISeries> SalesSeries { get; set; } = new();

        [ObservableProperty]
        public partial IEnumerable<ICartesianAxis> SalesXAxes { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<ISeries> RevenueSeries { get; set; } = new();

        [ObservableProperty]
        public partial IEnumerable<ICartesianAxis> RevenueXAxes { get; set; }

        [ObservableProperty]
        public partial bool IsChatVisible { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<ChatMessage> ChatMessages { get; set; } = new();

        [ObservableProperty]
        public partial string CurrentChatInput { get; set; } = string.Empty;

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

        partial void OnSelectedFilterChanged(string value)
        {
            if (value == "By Year")
            {
                IsDayVisible = false;
                IsMonthVisible = false;
            }
            else if (value == "By Month")
            {
                IsDayVisible = false;
                IsMonthVisible = true;
            }
            else
            {
                IsDayVisible = true;
                IsMonthVisible = true;
            }
            GenerateCharts();
        }

        partial void OnStartDateChanged(DateTimeOffset? value)
        {
            if (value.HasValue && EndDate.HasValue && value.Value > EndDate.Value)
            {
                StartDate = EndDate;
            }
            GenerateCharts();
        }

        partial void OnEndDateChanged(DateTimeOffset? value)
        {
            if (value.HasValue && StartDate.HasValue && value.Value < StartDate.Value)
            {
                EndDate = StartDate;
            }
            GenerateCharts();
        }

        private void GenerateCharts()
        {
            SalesSeries.Clear();
            RevenueSeries.Clear();

            var groupedOrders = GetGroupedOrders();

            if (!groupedOrders.Any())
            {
                SalesSeries.Add(new LineSeries<int> { Values = new[] { 0 }, Name = "Total Sales" });
                RevenueSeries.Add(new ColumnSeries<double> { Values = new[] { 0.0 }, Name = "Revenue (VND)" });
                RevenueSeries.Add(new ColumnSeries<double> { Values = new[] { 0.0 }, Name = "Profit (VND)" });
                
                var emptyAxis = new[] { new Axis { Labels = new[] { "No Data" }, LabelsRotation = 15 } };
                SalesXAxes = emptyAxis;
                RevenueXAxes = emptyAxis;
                return;
            }

            var labels = new List<string>();
            var totalSalesValues = new List<int>();
            var revenueValues = new List<double>();
            var profitValues = new List<double>();

            var activeProductSales = new Dictionary<int, List<int>>();
            foreach (var prod in ActiveProducts)
            {
                activeProductSales[prod.Id] = new List<int>();
            }

            foreach (var group in groupedOrders)
            {
                labels.Add(group.Label);
                
                int totalSalesCount = 0;
                double totalRevenue = 0;
                double totalProfit = 0;

                var productCounts = new Dictionary<int, int>();
                foreach (var prod in ActiveProducts) productCounts[prod.Id] = 0;

                foreach (var order in group.Orders)
                {
                    totalRevenue += (double)(order.TotalPrice ?? 0);
                    
                    if (order.OrderDetails != null)
                    {
                        foreach (var detail in order.OrderDetails)
                        {
                            totalSalesCount += detail.Quantity;
                            
                            if (detail.ProductId.HasValue && activeProductSales.ContainsKey(detail.ProductId.Value))
                            {
                                productCounts[detail.ProductId.Value] += detail.Quantity;
                            }

                            decimal basePrice = 0;
                            if (detail.ProductId.HasValue)
                            {
                                var prod = _allProducts.FirstOrDefault(p => p.Id == detail.ProductId.Value);
                                if (prod != null) basePrice = prod.BasePrice;
                            }
                            totalProfit += (double)((detail.Price - basePrice) * detail.Quantity);
                        }
                    }
                }

                totalSalesValues.Add(totalSalesCount);
                revenueValues.Add(totalRevenue);
                profitValues.Add(totalProfit);

                foreach (var prod in ActiveProducts)
                {
                    activeProductSales[prod.Id].Add(productCounts[prod.Id]);
                }
            }

            SalesSeries.Add(new LineSeries<int>
            {
                Values = totalSalesValues.ToArray(),
                Name = "Total Sales",
                Fill = null,
                GeometrySize = 8,
                LineSmoothness = 0.5
            });

            foreach (var prod in ActiveProducts)
            {
                SalesSeries.Add(new LineSeries<int>
                {
                    Values = activeProductSales[prod.Id].ToArray(),
                    Name = prod.Name,
                    Fill = null,
                    GeometrySize = 8,
                    LineSmoothness = 0.5
                });
            }

            RevenueSeries.Add(new ColumnSeries<double>
            {
                Values = revenueValues.ToArray(),
                Name = "Revenue (VND)",
            });

            RevenueSeries.Add(new ColumnSeries<double>
            {
                Values = profitValues.ToArray(),
                Name = "Profit (VND)",
            });

            var axes = new[] { new Axis { Labels = labels.ToArray(), LabelsRotation = 15 } };
            SalesXAxes = axes;
            RevenueXAxes = axes;
        }

        private IEnumerable<(string Label, List<Order> Orders)> GetGroupedOrders()
        {
            var filteredOrders = _allOrders.Where(o => 
                (!StartDate.HasValue || o.OrderAt.Date >= StartDate.Value.Date) &&
                (!EndDate.HasValue || o.OrderAt.Date <= EndDate.Value.Date)
            ).ToList();

            if (filteredOrders.Count == 0)
            {
                return new List<(string Label, List<Order> Orders)>();
            }

            IEnumerable<IGrouping<string, Order>> groupedOrders;

            if (SelectedFilter == "By Year")
            {
                groupedOrders = filteredOrders.GroupBy(o => o.OrderAt.ToString("yyyy"))
                                              .OrderBy(g => g.Key);
            }
            else if (SelectedFilter == "By Month")
            {
                groupedOrders = filteredOrders.GroupBy(o => o.OrderAt.ToString("MM/yyyy"))
                                              .OrderBy(g => DateTime.ParseExact(g.Key, "MM/yyyy", null));
            }
            else if (SelectedFilter == "By Week")
            {
                groupedOrders = filteredOrders.GroupBy(o => StartOfWeek(o.OrderAt, DayOfWeek.Monday).ToString("MM/dd/yyyy"))
                                              .OrderBy(g => DateTime.ParseExact(g.Key, "MM/dd/yyyy", null));
            }
            else // Custom Date Range
            {
                groupedOrders = filteredOrders.GroupBy(o => o.OrderAt.ToString("MM/dd/yyyy"))
                                              .OrderBy(g => DateTime.ParseExact(g.Key, "MM/dd/yyyy", null));
            }

            var result = new List<(string Label, List<Order> Orders)>();

            foreach (var group in groupedOrders)
            {
                string label = "";
                if (SelectedFilter == "By Year" || SelectedFilter == "By Month")
                {
                    label = group.Key;
                }
                else if (SelectedFilter == "By Week")
                {
                    var startOfWeek = DateTime.ParseExact(group.Key, "MM/dd/yyyy", null);
                    label = $"{startOfWeek:MM/dd} - {startOfWeek.AddDays(6):MM/dd}";
                }
                else
                {
                    var date = DateTime.ParseExact(group.Key, "MM/dd/yyyy", null);
                    label = date.ToString("MM/dd");
                }
                
                result.Add((label, group.ToList()));
            }

            return result;
        }

        private DateTime StartOfWeek(DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
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

        [RelayCommand]
        public void StartChat()
        {
            if (ChatMessages.Count == 0)
            {
                ChatMessages.Add(new ChatMessage 
                { 
                    Role = "AI", 
                    Text = "I have successfully analyzed the sales, revenue, and profit data. Based on the recent trends, it looks like there are opportunities to optimize product bundles. How can I assist you with your business planning today?" 
                });
            }
            IsChatVisible = true;
        }

        [RelayCommand]
        public void CloseChat()
        {
            IsChatVisible = false;
        }

        [RelayCommand]
        public async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(CurrentChatInput))
                return;

            var userText = CurrentChatInput;
            CurrentChatInput = string.Empty;

            ChatMessages.Add(new ChatMessage { Role = "User", Text = userText });

            // Simulate AI delay
            await Task.Delay(1000);

            ChatMessages.Add(new ChatMessage { Role = "AI", Text = "That's a great point. Considering the report data, focusing on top-performing products while re-evaluating underperforming ones could improve overall profit margins." });
        }
    }
}
