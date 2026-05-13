using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DominiShop.ViewModel;
using DominiShop.Model;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace DominiShop.View;

public sealed partial class CategoryPage : Page
{
    public CategoryViewModel ViewModel { get; }

    public CategoryPage()
    {
        ViewModel = App.Services.GetRequiredService<CategoryViewModel>();
        this.InitializeComponent();

        this.Loaded += (s, e) => { _ = ViewModel.LoadDataAsync(); };
    }

    private string GetDialogTitle(bool isEdit) => isEdit ? "Edit Category" : "Add New Category";

    private async void OnAddNewClick(object sender, RoutedEventArgs e)
    {
        ViewModel.AddNewCommand.Execute(null);
        EditDialog.XamlRoot = this.XamlRoot;
        await EditDialog.ShowAsync();
    }
    private async void OnRowSelected(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.SelectedCategory == null) return;

        var category = ViewModel.SelectedCategory;

        DetailDialog.XamlRoot = this.XamlRoot;
        var result = await DetailDialog.ShowAsync();

        ViewModel.SelectedCategory = null;

        if (result == ContentDialogResult.Primary) 
        {
            ViewModel.EditCommand.Execute(category);
            EditDialog.XamlRoot = this.XamlRoot;
            await EditDialog.ShowAsync();
        }
        else if (result == ContentDialogResult.Secondary) 
        {
            await ConfirmAndDelete(category);
        }
    }

    private async Task ConfirmAndDelete(Category category)
    {
        ContentDialog confirm = new ContentDialog
        {
            Title = "Confirm Delete",
            Content = $"Are you sure you want to delete the category '{category.Name}'? This action cannot be undone.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };

        if (await confirm.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.DeleteAsync(category);
        }
    }

    private async void OnDeleteRowClick(object sender, RoutedEventArgs e)
    {
        var category = (sender as Button)?.DataContext as Category;
        if (category != null)
        {
            ViewModel.SelectedCategory = null;
            await ConfirmAndDelete(category);
        }
    }

    private async void OnSaveDialogClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(ViewModel.EditingCategory.Name))
        {
            args.Cancel = true;
            return;
        }

        var deferral = args.GetDeferral();

        try
        {
            await ViewModel.SaveCommand.ExecuteAsync(null);
        }
        finally
        {
            deferral.Complete();
        }
    }
}