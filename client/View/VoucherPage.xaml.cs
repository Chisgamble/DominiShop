using DominiShop.Model;
using DominiShop.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DominiShop.View;


public sealed partial class VoucherPage : Page
{
    public VoucherViewModel ViewModel { get; }

    // Guards against OnRowSelected opening DetailDialog when the inline
    // delete button is clicked (which also triggers SelectionChanged).
    private bool _skipDetailDialog = false;

    public VoucherPage()
    {
        ViewModel = App.Services.GetRequiredService<VoucherViewModel>();
        this.InitializeComponent();
        this.Loaded += (s, e) => { _ = ViewModel.LoadDataAsync(); };
    }

    public string GetDialogTitle(bool isEditMode) => isEditMode ? "Update Voucher" : "Add New Voucher";

    // Add
    private async void OnAddNewClick(object sender, RoutedEventArgs e)
    {
        ErrorBanner.IsOpen = false;
        ViewModel.AddNewCommand.Execute(null);
        EditDialog.XamlRoot = this.XamlRoot;
        await EditDialog.ShowAsync();
    }

    // Row click to check detail dialog
    private async void OnRowSelected(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.SelectedVoucher == null) return;
        if (_skipDetailDialog)
        {
            _skipDetailDialog = false;  // consume the flag here, not in OnDeleteRowClick
            return;
        }
        var voucher = ViewModel.SelectedVoucher;

        DetailDialog.XamlRoot = this.XamlRoot;
        var result = await DetailDialog.ShowAsync();

        // Reset selection so clicking the same row again still fires
        ViewModel.SelectedVoucher = null;

        await Task.Yield();

        if (result == ContentDialogResult.Primary)          // Edit
        {
            ErrorBanner.IsOpen = false;
            ViewModel.EditCommand.Execute(voucher);
            EditDialog.XamlRoot = this.XamlRoot;
            await EditDialog.ShowAsync();
        }
        else if (result == ContentDialogResult.Secondary)   // Delete
        {
            await ConfirmAndDelete(voucher);
        }
    }

    // Save
    private async void OnSaveDialogClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ErrorBanner.IsOpen = false;
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(ViewModel.EditingVoucher.Code))
            errors.Add("Voucher Code is required.");

        if (ViewModel.EditingTypeIndex < 0)
            errors.Add("Please select a discount Type.");

        if (ViewModel.EditingMaxPerPerson < 0)
            errors.Add("Max per person cannot be negative.");

        if (ViewModel.EditingPercent <= 0 || ViewModel.EditingPercent > 100)
            errors.Add("Discount percent must be from 1 to 100.");

        if (ViewModel.EditingExpiryDate.HasValue &&
            ViewModel.EditingExpiryDate.Value.UtcDateTime < DateTime.UtcNow)
            errors.Add("Expiry date must be in the future.");

        if (errors.Count > 0)
        {
            args.Cancel = true;
            ErrorBanner.Message = string.Join("\n", errors);
            ErrorBanner.IsOpen = true;
            return;
        }

        var deferral = args.GetDeferral();
        try { await ViewModel.SaveCommand.ExecuteAsync(null); }
        finally { deferral.Complete(); }
    }

    // Delete
    private async Task ConfirmAndDelete(Voucher voucher)
    {
        var confirm = new ContentDialog
        {
            Title = "Confirm Delete",
            Content = $"Are you sure you want to delete voucher '{voucher.Code}'?\nThis action cannot be undone.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };

        if (await confirm.ShowAsync() == ContentDialogResult.Primary)
            await ViewModel.DeleteAsync(voucher);
    }

    // Inline delete button in DataGrid row
    private async void OnDeleteRowClick(object sender, RoutedEventArgs e)
    {
        var voucher = (sender as Button)?.DataContext as Voucher;
        if (voucher == null) return;

        _skipDetailDialog = true;        // OnRowSelected will consume + reset this
        ViewModel.SelectedVoucher = null;

        await ConfirmAndDelete(voucher);
    }
}