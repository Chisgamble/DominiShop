using DominiShop.Model;
using DominiShop.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace DominiShop.View;

public sealed partial class TaxPage : Page
{
    public TaxViewModel ViewModel { get; } = App.Services.GetRequiredService<TaxViewModel>();

    public TaxPage()
    {
        this.InitializeComponent();
        this.Loaded += (s, e) => ViewModel.LoadDataCommand.Execute(null);

    }

    private async void AddNew_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.AddNewCommand.Execute(null);
        EditDialog.XamlRoot = this.XamlRoot;
        await EditDialog.ShowAsync();
    }

    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        var tax = (sender as Button)?.DataContext as Tax;

        ViewModel.EditCommand.Execute(tax);
        EditDialog.XamlRoot = this.XamlRoot;
        await EditDialog.ShowAsync();
    }

    private async void EditDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = true;

        await ViewModel.SaveCommand.ExecuteAsync(null);

        if (!ViewModel.IsLoading && !ViewModel.HasError && !ViewModel.IsEditMode)
        {
            sender.Hide();
        }
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        var tax = (sender as Button)?.DataContext as Tax;
        ContentDialog deleteDialog = new ContentDialog
        {
            Title = "Confirm Delete",
            Content = $"Are you sure you want to delete {tax?.Name}?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot,
            DefaultButton = ContentDialogButton.Close
        };

        if (await deleteDialog.ShowAsync() == ContentDialogResult.Primary)
        {
            ViewModel.DeleteCommand.Execute(tax);
        }
    }
}