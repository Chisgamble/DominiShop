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
            ViewModel.ChatMessages.CollectionChanged += ChatMessages_CollectionChanged;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.IsChatVisible) && ViewModel.IsChatVisible)
            {
                ScrollChatToBottom();
            }
        }

        private void ChatMessages_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            ScrollChatToBottom();
        }

        private void ScrollChatToBottom()
        {
            if (ViewModel.ChatMessages.Count > 0)
            {
                // Delay slightly to allow the UI to update the items before scrolling
                _ = DispatcherQueue.TryEnqueue(() =>
                {
                    ChatListView.ScrollIntoView(ViewModel.ChatMessages.Last());
                });
            }
        }

        private async void ClearChat_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Clear Chat History",
                Content = "Are you sure you want to delete all chat messages? This cannot be undone.",
                PrimaryButtonText = "Clear",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                ViewModel.ClearChat();
            }
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

        private async void ExportData_Click(object sender, RoutedEventArgs e)
        {
            string folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DominiShop");
            System.IO.Directory.CreateDirectory(folder);
            string approvalFilePath = System.IO.Path.Combine(folder, "ai_approval.txt");
            bool hasApproved = System.IO.File.Exists(approvalFilePath);

            if (!hasApproved)
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
                    System.IO.File.WriteAllText(approvalFilePath, "true");
                    await ViewModel.StartChatAsync();
                }
            }
            else
            {
                await ViewModel.StartChatAsync();
            }
        }

        private bool _isDraggingTags = false;
        private double _lastPointerX;

        private void ProductTagsScrollViewer_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var sv = sender as ScrollViewer;
            if (sv != null)
            {
                _isDraggingTags = true;
                _lastPointerX = e.GetCurrentPoint(sv).Position.X;
                sv.CapturePointer(e.Pointer);
            }
        }

        private void ProductTagsScrollViewer_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var sv = sender as ScrollViewer;
            if (_isDraggingTags && sv != null)
            {
                double currentX = e.GetCurrentPoint(sv).Position.X;
                double delta = _lastPointerX - currentX;
                sv.ChangeView(sv.HorizontalOffset + delta, null, null);
                _lastPointerX = currentX;
            }
        }

        private void ProductTagsScrollViewer_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var sv = sender as ScrollViewer;
            if (sv != null)
            {
                _isDraggingTags = false;
                sv.ReleasePointerCapture(e.Pointer);
            }
        }
    }
}
