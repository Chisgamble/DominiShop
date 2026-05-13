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

    private string GetDialogTitle(bool isEdit) => isEdit ? "Cập nhật danh mục" : "Thêm danh mục mới";

    private async void OnAddNewClick(object sender, RoutedEventArgs e)
    {
        ViewModel.AddNewCommand.Execute(null);
        EditDialog.XamlRoot = this.XamlRoot;
        await EditDialog.ShowAsync();
    }
    // Xử lý khi nhấn vào dòng trên DataGrid
    private async void OnRowSelected(object sender, SelectionChangedEventArgs e)
    {
        // Nếu unselect thì bỏ qua
        if (ViewModel.SelectedCategory == null) return;

        // Lưu tạm category đang chọn
        var category = ViewModel.SelectedCategory;

        // Hiển thị Popup chi tiết
        DetailDialog.XamlRoot = this.XamlRoot;
        var result = await DetailDialog.ShowAsync();

        // RẤT QUAN TRỌNG: Reset vùng chọn để lần sau click lại dòng này nó vẫn nhận sự kiện
        ViewModel.SelectedCategory = null;

        // Xử lý hành động người dùng chọn trong Popup Chi tiết
        if (result == ContentDialogResult.Primary) // Nhấn Edit
        {
            ViewModel.EditCommand.Execute(category);
            EditDialog.XamlRoot = this.XamlRoot;
            await EditDialog.ShowAsync();
        }
        else if (result == ContentDialogResult.Secondary) // Nhấn Delete
        {
            await ConfirmAndDelete(category);
        }
    }

    // Tôi tách logic xác nhận xóa ra một hàm riêng để tái sử dụng
    // Gọi hàm này cho cả nút Xóa bên ngoài và nút Xóa trong DetailDialog
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

    // Sửa lại OnDeleteRowClick bên ngoài để dùng hàm chung ở trên
    private async void OnDeleteRowClick(object sender, RoutedEventArgs e)
    {
        var category = (sender as Button)?.DataContext as Category;
        if (category != null)
        {
            // Ngăn sự kiện SelectionChanged nhảy vào lúc ta bấm nút Xóa
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