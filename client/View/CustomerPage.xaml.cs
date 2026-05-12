using DominiShop.Model;
using DominiShop.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DominiShop.View;

public sealed partial class CustomerPage : Page
{
    public CustomerViewModel ViewModel { get; }

    // Guards against OnCustomerRowSelected opening DetailDialog when the
    // inline delete button fires (which also triggers SelectionChanged).
    private bool _skipDetailDialog = false;

    public CustomerPage()
    {
        ViewModel = App.Services.GetRequiredService<CustomerViewModel>();
        this.InitializeComponent();
        this.Loaded += (s, e) => { _ = ViewModel.LoadDataAsync(); };
    }

    // ── Dialog title helpers (used via x:Bind function syntax) ──────────────

    public string GetCustomerDialogTitle(bool isEditMode) =>
        isEditMode ? "Cập nhật khách hàng" : "Thêm khách hàng mới";

    public string GetTierDialogTitle(bool isEditMode) =>
        isEditMode ? "Cập nhật hạng" : "Thêm hạng mới";

    // ── Tab changed ──────────────────────────────────────────────────────────

    private void OnTabChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is TabView tv && tv.SelectedItem is TabViewItem tab)
            ViewModel.IsTierTab = (string)tab.Tag == "tiers";
    }

    // ════════════════════════════════════════════════════════════════════════
    //  CUSTOMER CRUD
    // ════════════════════════════════════════════════════════════════════════

    // Add button
    private async void OnAddCustomerClick(object sender, RoutedEventArgs e)
    {
        CustomerErrorBanner.IsOpen = false;
        ViewModel.AddNewCommand.Execute(null);
        CustomerEditDialog.XamlRoot = this.XamlRoot;
        await CustomerEditDialog.ShowAsync();
    }

    // Row click → detail dialog
    private async void OnCustomerRowSelected(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.SelectedCustomer == null) return;
        if (_skipDetailDialog)
        {
            _skipDetailDialog = false;
            return;
        }

        var customer = ViewModel.SelectedCustomer;

        CustomerDetailDialog.XamlRoot = this.XamlRoot;
        var result = await CustomerDetailDialog.ShowAsync();

        // Reset so clicking the same row again re-opens
        ViewModel.SelectedCustomer = null;
        await Task.Yield();

        if (result == ContentDialogResult.Primary)         // Edit
        {
            CustomerErrorBanner.IsOpen = false;
            ViewModel.EditCommand.Execute(customer);
            CustomerEditDialog.XamlRoot = this.XamlRoot;
            await CustomerEditDialog.ShowAsync();
        }
        else if (result == ContentDialogResult.Secondary)  // Delete
        {
            await ConfirmAndDeleteCustomer(customer);
        }
    }

    // Save customer dialog
    private async void OnSaveCustomerDialogClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        CustomerErrorBanner.IsOpen = false;
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(ViewModel.EditingCustomer.Username))
            errors.Add("Tên khách hàng là bắt buộc.");

        if (string.IsNullOrWhiteSpace(ViewModel.EditingCustomer.Phone))
            errors.Add("Số điện thoại là bắt buộc.");

        if (string.IsNullOrWhiteSpace(ViewModel.EditingCustomer.Email))
            errors.Add("Email là bắt buộc.");
        else if (!ViewModel.EditingCustomer.Email.Contains('@'))
            errors.Add("Email không hợp lệ.");

        if (errors.Count > 0)
        {
            args.Cancel = true;
            CustomerErrorBanner.Message = string.Join("\n", errors);
            CustomerErrorBanner.IsOpen  = true;
            return;
        }

        var deferral = args.GetDeferral();
        try { await ViewModel.SaveCommand.ExecuteAsync(null); }
        finally { deferral.Complete(); }
    }

    // Inline row delete button
    private async void OnDeleteCustomerRowClick(object sender, RoutedEventArgs e)
    {
        var customer = (sender as Button)?.DataContext as Customer;
        if (customer == null) return;

        _skipDetailDialog = true;
        ViewModel.SelectedCustomer = null;

        await ConfirmAndDeleteCustomer(customer);
    }

    private async Task ConfirmAndDeleteCustomer(Customer customer)
    {
        var confirm = new ContentDialog
        {
            Title             = "Xác nhận xoá",
            Content           = $"Bạn có chắc muốn xoá khách hàng '{customer.Username}' ({customer.Phone})?\nHành động này không thể hoàn tác.",
            PrimaryButtonText = "Xoá",
            CloseButtonText   = "Huỷ",
            XamlRoot          = this.XamlRoot
        };

        if (await confirm.ShowAsync() == ContentDialogResult.Primary)
            await ViewModel.DeleteAsync(customer);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  TIER CRUD
    // ════════════════════════════════════════════════════════════════════════

    // Add tier button
    private async void OnAddTierClick(object sender, RoutedEventArgs e)
    {
        TierErrorBanner.IsOpen = false;
        ViewModel.AddNewTierCommand.Execute(null);
        TierEditDialog.XamlRoot = this.XamlRoot;
        await TierEditDialog.ShowAsync();
    }

    // Edit button inside tier card
    private async void OnEditTierClick(object sender, RoutedEventArgs e)
    {
        var tier = (sender as Button)?.DataContext as CustomerTier;
        if (tier == null) return;

        TierErrorBanner.IsOpen = false;
        ViewModel.EditTierCommand.Execute(tier);
        TierEditDialog.XamlRoot = this.XamlRoot;
        await TierEditDialog.ShowAsync();
    }

    // Save tier dialog
    private async void OnSaveTierDialogClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        TierErrorBanner.IsOpen = false;
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(ViewModel.EditingTier.Name))
            errors.Add("Tên hạng là bắt buộc.");

        if (ViewModel.EditingTierMinPoint < 0)
            errors.Add("Điểm tối thiểu không được âm.");

        if (ViewModel.EditingTierPercent < 0 || ViewModel.EditingTierPercent > 100)
            errors.Add("Phần trăm giảm giá phải từ 0 đến 100.");

        if (errors.Count > 0)
        {
            args.Cancel = true;
            TierErrorBanner.Message = string.Join("\n", errors);
            TierErrorBanner.IsOpen  = true;
            return;
        }

        var deferral = args.GetDeferral();
        try { await ViewModel.SaveTierCommand.ExecuteAsync(null); }
        finally { deferral.Complete(); }
    }

    // Delete button inside tier card
    private async void OnDeleteTierClick(object sender, RoutedEventArgs e)
    {
        var tier = (sender as Button)?.DataContext as CustomerTier;
        if (tier == null) return;

        var confirm = new ContentDialog
        {
            Title             = "Xác nhận xoá hạng",
            Content           = $"Xoá hạng '{tier.Name}'? Khách hàng thuộc hạng này sẽ không còn hạng nữa.",
            PrimaryButtonText = "Xoá",
            CloseButtonText   = "Huỷ",
            XamlRoot          = this.XamlRoot
        };

        if (await confirm.ShowAsync() == ContentDialogResult.Primary)
            await ViewModel.DeleteTierAsync(tier);
    }
}
