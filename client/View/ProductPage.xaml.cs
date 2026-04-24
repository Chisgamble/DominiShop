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

        if (await confirm.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.DeleteAsync(product);
        }
    }

    private async void OnAddNewClick(object sender, RoutedEventArgs e)
    {
        ErrorBanner.IsOpen = false; // Ẩn lỗi cũ đi
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
        else if (result == ContentDialogResult.Secondary) 
        {
            await ConfirmAndDelete(product);
        }
    }

    private async void OnSaveDialogClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ErrorBanner.IsOpen = false;

        List<string> errorMessages = new List<string>();

        if (string.IsNullOrWhiteSpace(ViewModel.EditingProduct.Name))
            errorMessages.Add("• Product Name cannot be empty.");

        if (ViewModel.EditingProduct.CategoryId == null || ViewModel.EditingProduct.CategoryId == 0)
            errorMessages.Add("• Please select a Category.");

        if (ViewModel.EditingBasePrice < 0 || ViewModel.EditingSellPrice < 0)
            errorMessages.Add("• Prices cannot be negative.");

        if (ViewModel.EditingQuantity < 0)
            errorMessages.Add("• Quantity cannot be negative.");

        if (errorMessages.Count > 0)
        {
            args.Cancel = true;
            ErrorBanner.Message = string.Join("\n", errorMessages);
            ErrorBanner.IsOpen = true;
            return;
        }

        var deferral = args.GetDeferral();
        bool isSuccess = false;

        try
        {
            await ViewModel.SaveCommand.ExecuteAsync(null);

            if (!ViewModel.IsEditMode)
            {
                isSuccess = true;
            }
        }
        finally
        {
            deferral.Complete(); 
        }

        if (isSuccess)
        {
            ContentDialog successDialog = new ContentDialog
            {
                Title = "Success",
                Content = "New product has been saved successfully.",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await successDialog.ShowAsync();
        }
    }
}