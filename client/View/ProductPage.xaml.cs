using DominiShop.Model;
using DominiShop.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        if (ViewModel.EditingProduct.CategoryId == null || ViewModel.EditingProduct.CategoryId == 0) errors.Add("• Please select a Category.");
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
}