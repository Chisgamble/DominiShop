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

    public CustomerPage()
    {
        ViewModel = App.Services.GetRequiredService<CustomerViewModel>();
        this.InitializeComponent();
        this.Loaded += (s, e) => { _ = ViewModel.LoadDataAsync(); };
    }

    // ── Dialog title helpers (used via x:Bind function syntax) ──────────────

    public string GetCustomerDialogTitle(bool isEditMode) =>
        isEditMode ? "Update customer" : "Add new customer";

    public string GetTierDialogTitle(bool isEditMode) =>
        isEditMode ? "Update tier" : "Add new tier";

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

    // Inline row view button
    private async void OnViewCustomerRowClick(object sender, RoutedEventArgs e)
    {
        var customer = (sender as Button)?.DataContext as Customer;
        if (customer == null) return;

        ViewModel.SelectedCustomer = customer;

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
            errors.Add("Customer name is required.");

        if (string.IsNullOrWhiteSpace(ViewModel.EditingCustomer.Phone))
            errors.Add("Phone number is required.");

        if (string.IsNullOrWhiteSpace(ViewModel.EditingCustomer.Email))
            errors.Add("Email is required.");
        else if (!ViewModel.EditingCustomer.Email.Contains('@'))
            errors.Add("Invalid email.");

        if (errors.Count > 0)
        {
            args.Cancel = true;
            CustomerErrorBanner.Message = string.Join("\n", errors);
            CustomerErrorBanner.IsOpen = true;
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

        ViewModel.SelectedCustomer = null;

        await ConfirmAndDeleteCustomer(customer);
    }

    private async Task ConfirmAndDeleteCustomer(Customer customer)
    {
        var confirm = new ContentDialog
        {
            Title = "Confirm delete",
            Content = $"Are you sure you want to delete customer '{customer.Username}' ({customer.Phone})?\nThis action cannot be undone.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
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
            errors.Add("Tier name is required.");

        if (ViewModel.EditingTierMinPoint < 0)
            errors.Add("Minimum points cannot be negative.");

        if (ViewModel.EditingTierPercent < 0 || ViewModel.EditingTierPercent > 100)
            errors.Add("Discount percentage must be between 0 and 100.");

        if (errors.Count > 0)
        {
            args.Cancel = true;
            TierErrorBanner.Message = string.Join("\n", errors);
            TierErrorBanner.IsOpen = true;
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
            Title = "Confirm delete tier",
            Content = $"Delete tier '{tier.Name}'? Customers in this tier will no longer have a tier.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };

        if (await confirm.ShowAsync() == ContentDialogResult.Primary)
            await ViewModel.DeleteTierAsync(tier);
    }
}
