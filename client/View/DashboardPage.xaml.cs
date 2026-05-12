using DominiShop.Repository;
using DominiShop.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Linq;
using Windows.UI;

namespace DominiShop.View
{
    public sealed partial class DashboardPage : Page
    {
        private readonly DashboardViewModel _vm;

        public DashboardPage()
        {
            InitializeComponent();
            _vm = App.Services.GetRequiredService<DashboardViewModel>();
            Loaded += DashboardPage_Loaded;
        }

        // Lifecycle

        private async void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            SubtitleText.Text = $"Overview for {DateTime.Now:dddd, MMMM d, yyyy}";
            await LoadDashboardAsync();
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadDashboardAsync();
        }

        // Data loading

        private async System.Threading.Tasks.Task LoadDashboardAsync()
        {
            LoadingBar.Visibility = Visibility.Visible;
            ErrorInfoBar.IsOpen = false;

            await _vm.LoadAsync();

            LoadingBar.Visibility = Visibility.Collapsed;

            if (_vm.HasError)
            {
                ErrorInfoBar.Message = _vm.ErrorMessage;
                ErrorInfoBar.IsOpen = true;
                return;
            }

            // ── Stat cards
            TotalProductsText.Text = _vm.TotalProducts.ToString("N0");
            TodayOrdersText.Text   = _vm.TodayOrderCount.ToString("N0");
            TodayRevenueText.Text  = FormatCurrency(_vm.TodayRevenue);

            // ── Top product highlight
            TopProductNameText.Text = _vm.TopProductName;
            TopProductRevenueText.Text = FormatCurrency(_vm.TopProductTodayRevenue);

            // ── Low stock list
            LowStockListView.ItemsSource = _vm.LowStockProducts;
            LowStockEmptyText.Visibility = _vm.LowStockProducts.Count == 0
                ? Visibility.Visible : Visibility.Collapsed;

            // ── Best selling list
            BestSellingListView.ItemsSource = _vm.BestSellingProducts;
            BestSellingEmptyText.Visibility = _vm.BestSellingProducts.Count == 0
                ? Visibility.Visible : Visibility.Collapsed;

            // ── Revenue chart (drawn after layout pass)
            RevenueChartCanvas.Loaded -= Canvas_DrawChart;
            RevenueChartCanvas.Loaded += Canvas_DrawChart;

            // If canvas already laid out, draw immediately
            if (RevenueChartCanvas.ActualWidth > 0)
                DrawRevenueChart();
        }

        private void Canvas_DrawChart(object sender, RoutedEventArgs e) => DrawRevenueChart();

        // Line chart renderer

        private void DrawRevenueChart()
        {
            RevenueChartCanvas.Children.Clear();

            var data = _vm.DailyRevenueData;
            if (data == null || data.Count < 2) return;

            double canvasW = RevenueChartCanvas.ActualWidth;
            double canvasH = RevenueChartCanvas.ActualHeight;

            if (canvasW <= 0 || canvasH <= 0) return;

            const double padLeft   = 60;
            const double padRight  = 16;
            const double padTop    = 16;
            const double padBottom = 36;

            double chartW = canvasW - padLeft - padRight;
            double chartH = canvasH - padTop  - padBottom;

            decimal maxRevenue = data.Max(d => d.Revenue);
            if (maxRevenue == 0) maxRevenue = 1; // avoid divide-by-zero

            int count = data.Count;

            // ── Grid lines & Y-axis labels
            int gridLines = 4;
            for (int i = 0; i <= gridLines; i++)
            {
                double y = padTop + chartH - (chartH * i / gridLines);
                decimal value = maxRevenue * i / gridLines;

                var line = new Line
                {
                    X1 = padLeft, Y1 = y,
                    X2 = padLeft + chartW, Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromArgb(30, 128, 128, 128)),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 4, 4 }
                };
                RevenueChartCanvas.Children.Add(line);

                var label = new TextBlock
                {
                    Text = FormatCurrencyShort(value),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromArgb(160, 128, 128, 128))
                };
                Canvas.SetLeft(label, 0);
                Canvas.SetTop(label, y - 8);
                RevenueChartCanvas.Children.Add(label);
            }

            // ── Build polyline points
            var points = new PointCollection();
            for (int i = 0; i < count; i++)
            {
                double x = padLeft + (chartW * i / (count - 1));
                double y = padTop  + chartH - (double)(data[i].Revenue / maxRevenue) * chartH;
                points.Add(new Windows.Foundation.Point(x, y));
            }

            // ── Area fill (gradient-like with low opacity)
            var area = new Polygon
            {
                Fill = new SolidColorBrush(Color.FromArgb(25, 107, 138, 253)),
                StrokeThickness = 0
            };
            var areaPoints = new PointCollection();
            // bottom-left
            areaPoints.Add(new Windows.Foundation.Point(padLeft, padTop + chartH));
            foreach (var p in points) areaPoints.Add(p);
            // bottom-right
            areaPoints.Add(new Windows.Foundation.Point(padLeft + chartW, padTop + chartH));
            area.Points = areaPoints;
            RevenueChartCanvas.Children.Add(area);

            // ── Line
            var polyline = new Polyline
            {
                Points = points,
                Stroke = new SolidColorBrush(Color.FromArgb(255, 107, 138, 253)),
                StrokeThickness = 2.5,
                StrokeLineJoin = PenLineJoin.Round
            };
            RevenueChartCanvas.Children.Add(polyline);

            // ── Dots + X-axis date labels (every ~5 days)
            int labelEvery = Math.Max(1, count / 6);
            for (int i = 0; i < count; i++)
            {
                var pt = points[i];

                // dot
                var dot = new Ellipse
                {
                    Width = 6, Height = 6,
                    Fill = new SolidColorBrush(Color.FromArgb(255, 107, 138, 253)),
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 1.5
                };
                Canvas.SetLeft(dot, pt.X - 3);
                Canvas.SetTop(dot, pt.Y - 3);
                RevenueChartCanvas.Children.Add(dot);

                // x-axis label
                if (i % labelEvery == 0 || i == count - 1)
                {
                    var dateLabel = new TextBlock
                    {
                        Text = data[i].Date.ToString("dd/MM"),
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.FromArgb(160, 128, 128, 128))
                    };
                    Canvas.SetLeft(dateLabel, pt.X - 14);
                    Canvas.SetTop(dateLabel, padTop + chartH + 6);
                    RevenueChartCanvas.Children.Add(dateLabel);
                }
            }
        }

        // Helpers

        private static string FormatCurrency(decimal value)
        {
            // Format as Vietnamese Dong or adapt to your locale
            if (value >= 1_000_000)
                return $"{value / 1_000_000:N1}M ₫";
            if (value >= 1_000)
                return $"{value / 1_000:N0}K ₫";
            return $"{value:N0} ₫";
        }

        private static string FormatCurrencyShort(decimal value)
        {
            if (value >= 1_000_000)
                return $"{value / 1_000_000:N1}M";
            if (value >= 1_000)
                return $"{value / 1_000:N0}K";
            return value.ToString("N0");
        }
    }
}
