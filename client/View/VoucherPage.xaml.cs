using DominiShop.Model;
using DominiShop.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace DominiShop.View;

public sealed partial class VoucherPage : Page
{
    public VoucherViewModel ViewModel { get; } =
        App.Services.GetRequiredService<VoucherViewModel>();

    public VoucherPage()
    {
        InitializeComponent();
        _ = ViewModel.LoadVouchersCommand.ExecuteAsync(null);
    }

    // edit voucher click
    private void VoucherList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Voucher voucher)
            ViewModel.SelectForEditCommand.Execute(voucher);
    }

    // delete voucher click
    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        var voucher = ViewModel.SelectedVoucher;
        if (voucher == null) return;

        var dialog = new ContentDialog
        {
            Title = "Delete Voucher",
            Content = $"Are you sure you want to delete voucher \"{voucher.Code}\"?\nThis action cannot be undone.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
            await ViewModel.DeleteCommand.ExecuteAsync(voucher);
    }
}
