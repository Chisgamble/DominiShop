using DominiShop.Model;
using DominiShop.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WinRT.Interop;
using Windows.Storage.Pickers;

using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.Storage;
using System;
using System.IO;
using System.Threading.Tasks;

using WinRT.Interop;

namespace DominiShop.View;

public sealed partial class ProductPage : Page
{
    public ProductViewModel ViewModel { get; }

    public ProductPage()
    {
        ViewModel = App.Services.GetRequiredService<ProductViewModel>();
        this.InitializeComponent();
        this.Loaded += (s, e) => { _ = ViewModel.LoadDataAsync(); };
    }

    public string GetDialogTitle(bool isEdit) => isEdit ? "Update Product" : "Add New Product";

    private async void OnAddNewClick(object sender, RoutedEventArgs e)
    {
        ErrorBanner.IsOpen = false;
        ViewModel.AddNewCommand.Execute(null);
        EditDialog.XamlRoot = this.XamlRoot;
        await EditDialog.ShowAsync();
    }

    private async void OnRowSelected(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.SelectedProduct == null) return;
        var product = ViewModel.SelectedProduct;
        DetailDialog.XamlRoot = this.XamlRoot;
        var result = await DetailDialog.ShowAsync();
        ViewModel.SelectedProduct = null;

        if (result == ContentDialogResult.Primary)
        {
            ErrorBanner.IsOpen = false;
            ViewModel.EditCommand.Execute(product);
            EditDialog.XamlRoot = this.XamlRoot;
            await EditDialog.ShowAsync();
        }
        else if (result == ContentDialogResult.Secondary) await ConfirmAndDelete(product);
    }

    private async Task ConfirmAndDelete(Product product)
    {
        ContentDialog confirm = new ContentDialog
        {
            Title = "Confirm Delete",
            Content = $"Are you sure you want to delete '{product.Name}'? This action cannot be undone.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };
        if (await confirm.ShowAsync() == ContentDialogResult.Primary) await ViewModel.DeleteAsync(product);
    }

    private async void OnSaveDialogClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ErrorBanner.IsOpen = false;
        List<string> errors = new();

        if (string.IsNullOrWhiteSpace(ViewModel.EditingProduct.Name)) errors.Add("• Product Name is required.");

        if (ViewModel.EditingCategoryId == null || ViewModel.EditingCategoryId == 0) errors.Add("• Please select a Category.");

        if (ViewModel.EditingBasePrice < 0 || ViewModel.EditingSellPrice < 0) errors.Add("• Prices cannot be negative.");

        double currentSum = ViewModel.EditingInStock + ViewModel.EditingSold;
        if (currentSum != ViewModel.EditingTotalQuantity)
        {
            errors.Add($"• The sum of In Stock ({ViewModel.EditingInStock}) and Sold ({ViewModel.EditingSold}) must equal the Required Total ({ViewModel.EditingTotalQuantity}). Current sum is {currentSum}.");
        }

        if (errors.Count > 0)
        {
            args.Cancel = true;
            ErrorBanner.Message = string.Join("\n", errors);
            ErrorBanner.IsOpen = true;
            return;
        }

        var deferral = args.GetDeferral();
        bool isSuccess = false;
        try
        {
            await ViewModel.SaveCommand.ExecuteAsync(null);
            if (!ViewModel.IsEditMode) isSuccess = true;
        }
        finally { deferral.Complete(); }

        if (isSuccess)
        {
            ContentDialog success = new ContentDialog
            {
                Title = "Success",
                Content = "Product saved successfully!",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await success.ShowAsync();
        }
    }

    private async void OnQuickAddCategoryClick(object sender, RoutedEventArgs e)
    {
        EditDialog.Hide();

        ViewModel.QuickAddCategory = new Category();
        QuickCategoryError.IsOpen = false;
        QuickAddCategoryDialog.XamlRoot = this.XamlRoot;

        await QuickAddCategoryDialog.ShowAsync();

        await EditDialog.ShowAsync();
    }

    private async void OnQuickAddCategorySaveClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        QuickCategoryError.IsOpen = false;

        if (string.IsNullOrWhiteSpace(ViewModel.QuickAddCategory.Name))
        {
            args.Cancel = true; 
            QuickCategoryError.Message = "Category Name is required."; 
            QuickCategoryError.IsOpen = true;
            return;
        }

        var deferral = args.GetDeferral(); 
        try
        {
            var (success, error) = await ViewModel.SaveQuickCategoryAsync();
            if (!success)
            {
                args.Cancel = true; 
                QuickCategoryError.Message = $"Failed to create category: {error}";
                QuickCategoryError.IsOpen = true;
            }
        }
        finally
        {
            deferral.Complete();
        }
    }


    private async void OnExportClick(object sender, RoutedEventArgs e)
    {
        var picker = new FileSavePicker();

        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeChoices.Add("Excel Workbook", new List<string>() { ".xlsx" });
        picker.SuggestedFileName = $"Products_Export_{DateTime.Now:yyyyMMdd}";

        var file = await picker.PickSaveFileAsync();
        if (file != null)
        {
            await ViewModel.ExportExcelAsync(file.Path);

            ContentDialog success = new ContentDialog
            {
                Title = "Success",
                Content = "Excel data exported successfully!",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await success.ShowAsync();
        }
    }

    private async void OnImportClick(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();

        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        picker.ViewMode = PickerViewMode.Thumbnail;
        picker.FileTypeFilter.Add(".xlsx");

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            var (success, error) = await ViewModel.ImportExcelAsync(file.Path);

            ContentDialog resultDialog = new ContentDialog
            {
                Title = success ? "Success" : "Error",
                Content = success
                    ? "Excel data imported successfully! Product quantities have been automatically updated."
                    : $"Failed to import Excel data: {error}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await resultDialog.ShowAsync();
        }
    }

    private async void OnPickImagesClick(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".jpeg");

        var files = await picker.PickMultipleFilesAsync();
        if (files.Count > 0)
        {
            var paths = new List<string>();
            var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

            foreach (var file in files)
            {
                var copiedFile = await file.CopyAsync(localFolder, file.Name, Windows.Storage.NameCollisionOption.GenerateUniqueName);
                paths.Add(copiedFile.Path);
            }
            await ViewModel.PickImagesAsync(paths);
        }
    }

    private void OnRemoveImageClick(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        if (btn?.DataContext is string url)
        {
            ViewModel.RemoveImage(url);
        }
    }
}