using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominiShop.Model;
using DominiShop.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace DominiShop.ViewModel;

public partial class CategoryViewModel(CategoryService categoryService) : BaseViewModel
{
    private readonly CategoryService _service = categoryService;
    private List<Category> _masterCategories = new();

    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial bool IsEditMode { get; set; }

    // --- TÌM KIẾM & SẮP XẾP ---
    [ObservableProperty] public partial string SearchText { get; set; } = string.Empty;
    [ObservableProperty] public partial string SelectedSortOption { get; set; } = "Name (A-Z)";

    public List<string> SortOptions { get; } = new()
    {
        "Name (A-Z)",
        "Name (Z-A)",
        "Latest Created",
        "Oldest Created",
        "Products (Most)",
        "Products (Least)"
    };

    public ObservableCollection<Category> FilteredCategories { get; } = new();

    partial void OnSearchTextChanged(string value) => FilterData();
    partial void OnSelectedSortOptionChanged(string value) => FilterData();

    // --- QUẢN LÝ CHỌN ITEM ---
    [ObservableProperty] public partial Category? SelectedCategory { get; set; }
    [ObservableProperty] public partial Category EditingCategory { get; set; } = new();

    // Biến bool này là CHÌA KHÓA để giao diện ẩn/hiện chính xác
    public bool HasSelectedCategory => SelectedCategory != null;

    partial void OnSelectedCategoryChanged(Category? value)
    {
        OnPropertyChanged(nameof(HasSelectedCategory));
        // Khi chọn một item mới, luôn ép nó về chế độ Xem Chi Tiết (thoát chế độ Sửa)
        if (value != null) IsEditMode = false;
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _service.GetCategoriesAsync();
            if (result.Success && result.Data != null)
            {
                _masterCategories = result.Data;
                // Gọi hàm này để đẩy dữ liệu vào FilteredCategories
                FilterData();
            }
        }
        finally { IsLoading = false; }
    }

    private void FilterData()
    {
        var query = _masterCategories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(c => c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                    (c.Note != null && c.Note.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));
        }

        query = SelectedSortOption switch
        {
            "Name (A-Z)" => query.OrderBy(c => c.Name),
            "Name (Z-A)" => query.OrderByDescending(c => c.Name),
            "Latest Created" => query.OrderByDescending(c => c.CreatedAt),
            "Oldest Created" => query.OrderBy(c => c.CreatedAt),
            "Products (Most)" => query.OrderByDescending(c => c.Products != null ? c.Products.Count : 0),
            "Products (Least)" => query.OrderBy(c => c.Products != null ? c.Products.Count : 0),
            _ => query.OrderBy(c => c.Name)
        };

        var results = query.ToList();

        FilteredCategories.Clear();
        foreach (var item in results)
        {
            FilteredCategories.Add(item);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(EditingCategory.Name)) return;

        IsLoading = true;
        bool isSuccess = false;

        if (EditingCategory.Id == 0)
        {
            var res = await _service.CreateCategoryAsync(EditingCategory);
            isSuccess = res.Success;
        }
        else
        {
            var res = await _service.UpdateCategoryAsync(EditingCategory);
            isSuccess = res.Success;
        }

        if (isSuccess)
        {
            IsEditMode = false;
            // Gọi lại hàm Load để refresh ObservableCollection ngay lập tức
            await LoadDataAsync();
        }
        IsLoading = false;
    }

    [RelayCommand]
    public async Task DeleteAsync(Category? category)
    {
        if (category == null) return;

        IsLoading = true;
        var res = await _service.DeleteCategoryAsync(category.Id);
        if (res.Success)
        {
            SelectedCategory = null;
            IsEditMode = false;
            // Refresh lại danh sách sau khi xóa thành công
            await LoadDataAsync();
        }
        IsLoading = false;
    }

    [RelayCommand]
    private void AddNew() { EditingCategory = new Category(); IsEditMode = true; SelectedCategory = null; }

    [RelayCommand]
    private void Edit(Category? category)
    {
        if (category == null) return;
        EditingCategory = new Category { Id = category.Id, Name = category.Name, Note = category.Note };
        IsEditMode = true;
    }

    [RelayCommand]
    private void Cancel()
    {
        IsEditMode = false;
        // Nếu đang tạo mới mà bấm Hủy thì xóa bỏ vùng chọn luôn
        if (EditingCategory.Id == 0) SelectedCategory = null;
    }
}