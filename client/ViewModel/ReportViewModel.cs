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
        private readonly CategoryService _categoryService;
        private readonly AIService _aiService;

        private List<Order> _allOrders = new();
        private List<Product> _allProducts = new();
        private List<Category> _allCategories = new();
        private string _systemPromptContext = string.Empty;
        private readonly string _chatHistoryPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DominiShop", "chat_history.json");

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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsTypingVisibility))]
        public partial bool IsTyping { get; set; }

        public Microsoft.UI.Xaml.Visibility IsTypingVisibility => IsTyping ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

        public ReportViewModel(ProductService productService, OrderService orderService, CategoryService categoryService, AIService aiService)
        {
            _productService = productService;
            _orderService = orderService;
            _categoryService = categoryService;
            _aiService = aiService;

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
            var cTask = _categoryService.GetCategoriesAsync();

            await Task.WhenAll(pTask, oTask, cTask);

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

            var cRes = await cTask;
            if (cRes.Success && cRes.Data != null)
            {
                _allCategories = cRes.Data;
            }

            LoadChatHistory();
            GenerateCharts();
        }

        private void LoadChatHistory()
        {
            try
            {
                if (System.IO.File.Exists(_chatHistoryPath))
                {
                    var json = System.IO.File.ReadAllText(_chatHistoryPath);
                    var messages = System.Text.Json.JsonSerializer.Deserialize<List<ChatMessage>>(json);
                    if (messages != null)
                    {
                        ChatMessages.Clear();
                        foreach (var msg in messages)
                        {
                            ChatMessages.Add(msg);
                        }
                    }
                }
            }
            catch { /* Ignore load errors */ }
        }

        private void SaveChatHistory()
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(ChatMessages);
                System.IO.File.WriteAllText(_chatHistoryPath, json);
            }
            catch { /* Ignore save errors */ }
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
        public void OpenChat()
        {
            IsChatVisible = true;
        }

        [RelayCommand]
        public async Task StartChatAsync()
        {
            IsChatVisible = true;
            
            _systemPromptContext = """
                You are an AI business analyst assistant for a Vietnamese retail shop management system called DominiShop.
                
                IMPORTANT RULES:
                - All monetary values in the data (prices, revenue, profit, TotalPrice) are in Vietnamese Dong (VND). Never convert or assume another currency.
                - Always display monetary values with the "₫" symbol or write "VND" after numbers (e.g. 150,000 ₫).
                - The data belongs to a single shop owner. Analyze only the data provided.
                - Respond concisely and use markdown formatting (headers, bullet points, bold) for clarity.
                - When making recommendations, be specific and actionable.
                - NEVER use raw HTML tags (such as <ul>, <li>, <br>, <b>, <strong>, etc.) anywhere in your response. Use only standard markdown syntax.
                - Inside markdown table cells, NEVER use lists. Use plain text separated by commas or semicolons instead.
                
                The following is a CSV snapshot of the owner's current shop data:
                
                """ + GenerateCsvData();

            IsTyping = true;
            ChatMessages.Add(new ChatMessage { Role = "User", Text = "Please analyze my report data and provide a summary and recommendations." });
            SaveChatHistory();
            
            var reply = await _aiService.SendMessageAsync(ChatMessages.TakeLast(15), _systemPromptContext);
            
            ChatMessages.Add(new ChatMessage { Role = "AI", Text = StripHtmlTags(reply) });
            IsTyping = false;
            SaveChatHistory();
        }

        [RelayCommand]
        public void CloseChat()
        {
            IsChatVisible = false;
        }

        [RelayCommand]
        public void ClearChat()
        {
            ChatMessages.Clear();
            try
            {
                if (System.IO.File.Exists(_chatHistoryPath))
                    System.IO.File.Delete(_chatHistoryPath);
            }
            catch { /* Ignore */ }
        }

        [RelayCommand]
        public async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(CurrentChatInput))
                return;

            var userText = CurrentChatInput;
            CurrentChatInput = string.Empty;

            ChatMessages.Add(new ChatMessage { Role = "User", Text = userText });
            SaveChatHistory();

            IsTyping = true;
            var reply = await _aiService.SendMessageAsync(ChatMessages.TakeLast(15), _systemPromptContext);
            ChatMessages.Add(new ChatMessage { Role = "AI", Text = StripHtmlTags(reply) });
            IsTyping = false;
            SaveChatHistory();
        }

        private static string StripHtmlTags(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            // Replace common block-level HTML that breaks markdown rendering
            var result = System.Text.RegularExpressions.Regex.Replace(input, @"<ul[^>]*>", "\n");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"</ul>", "\n");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"<li[^>]*>", "- ");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"</li>", "\n");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"<br\s*/?>", "\n");
            // Strip any remaining HTML tags
            result = System.Text.RegularExpressions.Regex.Replace(result, @"<[^>]+>", string.Empty);
            return result.Trim();
        }

        private string GenerateCsvData()
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("--- PRODUCTS ---");
            sb.AppendLine("Id,Name,BasePrice,CategoryId");
            foreach (var p in _allProducts)
            {
                sb.AppendLine($"{p.Id},\"{p.Name}\",{p.BasePrice},{p.CategoryId}");
            }

            sb.AppendLine("\n--- CATEGORIES ---");
            sb.AppendLine("Id,Name");
            foreach (var c in _allCategories)
            {
                sb.AppendLine($"{c.Id},\"{c.Name}\"");
            }

            sb.AppendLine("\n--- ORDERS ---");
            sb.AppendLine("Id,OrderAt,TotalPrice");
            foreach (var o in _allOrders)
            {
                sb.AppendLine($"{o.Id},{o.OrderAt:yyyy-MM-dd HH:mm},{o.TotalPrice}");
            }

            sb.AppendLine("\n--- ORDER DETAILS ---");
            sb.AppendLine("OrderId,ProductId,Quantity,Price");
            foreach (var o in _allOrders)
            {
                if (o.OrderDetails != null)
                {
                    foreach (var d in o.OrderDetails)
                    {
                        sb.AppendLine($"{o.Id},{d.ProductId},{d.Quantity},{d.Price}");
                    }
                }
            }

            return sb.ToString();
        }
    }
}
