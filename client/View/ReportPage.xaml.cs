using DominiShop.Model;
using DominiShop.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;

namespace DominiShop.View
{
    public sealed partial class ReportPage : Page
    {
        public ReportViewModel ViewModel { get; }

        public ReportPage()
        {
            this.InitializeComponent();
            ViewModel = App.Services.GetRequiredService<ReportViewModel>();
            
            this.Loaded += ReportPage_Loaded;
        }

        private async void ReportPage_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.InitializeAsync();
        }



        private void ProductSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var suggestions = ViewModel.AvailableProducts
                    .Where(p => p.Name.Contains(sender.Text, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                sender.ItemsSource = suggestions;
            }
        }

        private void ProductSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is Product selectedProduct)
            {
                ViewModel.AddProductFilter(selectedProduct);
                sender.Text = string.Empty;
                sender.ItemsSource = null;
            }
        }

        private void RemoveFilter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Product product)
            {
                ViewModel.RemoveProductFilter(product);
            }
        }

        private async void AnalyzeWithAI_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "AI Analysis Approval",
                Content = "This feature will export the current report data (sales, revenue, profit) and send it to our AI service for analysis. Do you approve?",
                PrimaryButtonText = "Approve & Analyze",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var resultDialog = new ContentDialog
                {
                    Title = "Analysis Complete",
                    Content = "The AI has analyzed the report successfully! (Placeholder response)",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await resultDialog.ShowAsync();
            }
        }
    }
}
