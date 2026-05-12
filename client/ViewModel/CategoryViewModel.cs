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

public partial class CategoryViewModel(CategoryService categoryService, SettingService settingService) : BaseViewModel
{
    private readonly CategoryService _service = categoryService;
    private readonly SettingService _settingService = settingService;
    private List<Category> _masterCategories = new();

    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial bool IsEditMode { get; set; }

    [ObservableProperty] public partial string SearchText { get; set; } = string.Empty;
    [ObservableProperty] public partial string SelectedSortOption { get; set; } = "Name (A-Z)";


    [ObservableProperty] public partial int CurrentPage { get; set; } = 1;
    [ObservableProperty] public partial int TotalPages { get; set; } = 1;
    [ObservableProperty] public partial int PageSize { get; set; }

    public List<int> PageSizeOptions { get; } = new() { 5, 10, 15, 20 };
    public bool CanGoPrevious => CurrentPage > 1;
    public bool CanGoNext => CurrentPage < TotalPages;
    public string PagingInfo => $"Page {CurrentPage} of {TotalPages}";

    private List<Category> _currentFilteredList = new();
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

    [ObservableProperty] public partial Category? SelectedCategory { get; set; }
    [ObservableProperty] public partial Category EditingCategory { get; set; } = new();

    public bool HasSelectedCategory => SelectedCategory != null;

    partial void OnSelectedCategoryChanged(Category? value)
    {
        OnPropertyChanged(nameof(HasSelectedCategory));
        if (value != null) IsEditMode = false;
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        IsLoading = true;

        int savedPageSize = _settingService.GetCategoryPageSize();
        if (PageSize != savedPageSize)
        {
            PageSize = savedPageSize;
        }

        try
        {
            var result = await _service.GetCategoriesAsync();
            if (result.Success && result.Data != null)
            {
                _masterCategories = result.Data;
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

        _currentFilteredList = query.ToList();
        CurrentPage = 1;
        ApplyPaging();
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
        if (EditingCategory.Id == 0) SelectedCategory = null;
    }

    partial void OnPageSizeChanged(int value)
    {
        if (value > 0)
        {
            _settingService.SaveCategoryPageSize(value);
        }

        CurrentPage = 1;
        ApplyPaging();
    }

    [RelayCommand]
    private void NextPage() { if (CanGoNext) { CurrentPage++; ApplyPaging(); } }

    [RelayCommand]
    private void PreviousPage() { if (CanGoPrevious) { CurrentPage--; ApplyPaging(); } }

    private void ApplyPaging()
    {
        TotalPages = (int)Math.Ceiling(_currentFilteredList.Count / (double)PageSize);
        if (TotalPages == 0) TotalPages = 1;

        var pagedData = _currentFilteredList
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        FilteredCategories.Clear();
        foreach (var item in pagedData) FilteredCategories.Add(item);

        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(PagingInfo));
    }
}