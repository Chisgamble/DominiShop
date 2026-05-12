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

public partial class ProductViewModel(ProductService productService, CategoryService categoryService) : BaseViewModel
{
    private readonly ProductService _productService = productService;
    private readonly CategoryService _categoryService = categoryService;

    private List<Product> _masterProducts = new();

    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial bool IsEditMode { get; set; }

    public ObservableCollection<Product> FilteredProducts { get; } = new();
    [ObservableProperty] public partial ObservableCollection<Category> AvailableCategories { get; set; } = new();
    [ObservableProperty] public partial ObservableCollection<Category> FilterCategories { get; set; } = new();

    [ObservableProperty] public partial string SearchText { get; set; } = string.Empty;
    [ObservableProperty] public partial Category? SelectedFilterCategory { get; set; }
    [ObservableProperty] public partial string SelectedSortOption { get; set; } = "Name (A-Z)";


    [ObservableProperty] public partial int CurrentPage { get; set; } = 1;
    [ObservableProperty] public partial int TotalPages { get; set; } = 1;
    [ObservableProperty] public partial int PageSize { get; set; } = 10;

    public List<int> PageSizeOptions { get; } = new() { 10, 20, 50, 100 };
    public bool CanGoPrevious => CurrentPage > 1;
    public bool CanGoNext => CurrentPage < TotalPages;
    public string PagingInfo => $"Page {CurrentPage} of {TotalPages}";

    private List<Product> _currentFilteredList = new();

    public List<string> SortOptions { get; } = new()
    {
        "Name (A-Z)", "Name (Z-A)", "Price (Low-High)", "Price (High-Low)", "Highest Stock", "Best Seller"
    };

    partial void OnSearchTextChanged(string value) => FilterData();
    partial void OnSelectedFilterCategoryChanged(Category? value) => FilterData();
    partial void OnSelectedSortOptionChanged(string value) => FilterData();

    [ObservableProperty] public partial Product? SelectedProduct { get; set; }
    [ObservableProperty] public partial Product EditingProduct { get; set; } = new();

    // Bridging variables for UI Input
    [ObservableProperty] public partial int? EditingCategoryId { get; set; }

    [ObservableProperty] public partial double EditingBasePrice { get; set; }
    [ObservableProperty] public partial double EditingSellPrice { get; set; }
    [ObservableProperty] public partial double EditingInStock { get; set; }
    [ObservableProperty] public partial double EditingSold { get; set; }
    [ObservableProperty] public partial double EditingTotalQuantity { get; set; }

    [ObservableProperty] public partial Category QuickAddCategory { get; set; } = new();

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var categoryResult = await _categoryService.GetCategoriesAsync();
            var productResult = await _productService.GetProductsAsync();

            if (categoryResult.Success && categoryResult.Data != null)
            {
                AvailableCategories.Clear();
                FilterCategories.Clear();
                FilterCategories.Add(new Category { Id = 0, Name = "All Categories" });
                foreach (var c in categoryResult.Data)
                {
                    AvailableCategories.Add(c);
                    FilterCategories.Add(c);
                }
                if (SelectedFilterCategory == null) SelectedFilterCategory = FilterCategories.First();
            }

            if (productResult.Success && productResult.Data != null)
            {
                _masterProducts = productResult.Data;
                FilterData();
            }
        }
        finally { IsLoading = false; }
    }

    private void FilterData()
    {
        var query = _masterProducts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(p => p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedFilterCategory != null && SelectedFilterCategory.Id != 0)
        {
            query = query.Where(p => p.CategoryId == SelectedFilterCategory.Id);
        }

        query = SelectedSortOption switch
        {
            "Name (A-Z)" => query.OrderBy(p => p.Name),
            "Name (Z-A)" => query.OrderByDescending(p => p.Name),
            "Price (Low-High)" => query.OrderBy(p => p.Price),
            "Price (High-Low)" => query.OrderByDescending(p => p.Price),
            "Highest Stock" => query.OrderByDescending(p => p.Quantity),
            "Best Seller" => query.OrderByDescending(p => p.Sold),
            _ => query.OrderBy(p => p.Name)
        };

        _currentFilteredList = query.ToList();
        CurrentPage = 1;
        ApplyPaging();
    }

    [RelayCommand]
    private void AddNew()
    {
        EditingProduct = new Product();
        EditingBasePrice = 0;
        EditingSellPrice = 0;
        EditingInStock = 0;
        EditingSold = 0;
        EditingTotalQuantity = 0;

        EditingCategoryId = AvailableCategories.FirstOrDefault()?.Id;

        IsEditMode = true;
        SelectedProduct = null;
    }

    [RelayCommand]
    private void Edit(Product? product)
    {
        if (product == null) return;
        EditingProduct = new Product
        {
            Id = product.Id,
            Name = product.Name,
            Note = product.Note,
            BasePrice = product.BasePrice,
            Price = product.Price,
            Quantity = product.Quantity,
            CategoryId = product.CategoryId,
            Sold = product.Sold
        };

        EditingBasePrice = (double)product.BasePrice;
        EditingSellPrice = (double)product.Price;
        EditingInStock = product.Quantity;
        EditingSold = product.Sold;
        EditingTotalQuantity = EditingInStock + EditingSold;

        EditingCategoryId = product.CategoryId;

        IsEditMode = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsLoading = true;
        EditingProduct.BasePrice = (decimal)EditingBasePrice;
        EditingProduct.Price = (decimal)EditingSellPrice;
        EditingProduct.Quantity = (int)EditingInStock;
        EditingProduct.Sold = (int)EditingSold;

        EditingProduct.CategoryId = EditingCategoryId ?? 0;

        bool isSuccess = EditingProduct.Id == 0
            ? (await _productService.CreateProductAsync(EditingProduct)).Success
            : (await _productService.UpdateProductAsync(EditingProduct)).Success;

        if (isSuccess)
        {
            IsEditMode = false;
            await LoadDataAsync();
        }
        IsLoading = false;
    }

    [RelayCommand]
    public async Task DeleteAsync(Product? product)
    {
        if (product == null) return;
        IsLoading = true;
        if ((await _productService.DeleteProductAsync(product.Id)).Success)
        {
            SelectedProduct = null;
            await LoadDataAsync();
        }
        IsLoading = false;
    }


    [RelayCommand]
    public async Task<(bool Success, string Error)> SaveQuickCategoryAsync()
    {
        IsLoading = true;
        var res = await _categoryService.CreateCategoryAsync(QuickAddCategory);

        if (res.Success && res.Data != null)
        {
            AvailableCategories.Add(res.Data);
            FilterCategories.Add(res.Data);

            EditingCategoryId = res.Data.Id;

            IsLoading = false;
            return (true, string.Empty);
        }

        IsLoading = false;
        return (false, res.Error ?? "Category creation error");
    }



    partial void OnPageSizeChanged(int value)
    {
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

        FilteredProducts.Clear();
        foreach (var item in pagedData) FilteredProducts.Add(item);

        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(PagingInfo));
    }
}