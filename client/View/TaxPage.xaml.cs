using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using DominiShop.Model;
using DominiShop.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
namespace DominiShop.View;

public sealed partial class TaxPage : Page
{
    public TaxViewModel ViewModel { get; } = App.Services.GetRequiredService<TaxViewModel>();

    public TaxPage()
    {
        this.InitializeComponent();
        this.Loaded += (s, e) => ViewModel.LoadDataCommand.Execute(null);

        ViewModel.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.IsEditMode) && ViewModel.IsEditMode)
            {
                await EditDialog.ShowAsync();
            }
        };
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        var tax = (sender as Button)?.DataContext as Tax;
        ViewModel.EditCommand.Execute(tax);
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
