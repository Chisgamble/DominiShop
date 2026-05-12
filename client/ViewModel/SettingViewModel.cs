using CommunityToolkit.Mvvm.ComponentModel;
using DominiShop.Service;
using System.Collections.Generic;

namespace DominiShop.ViewModel;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly SettingService _settingService;

    public List<int> PageSizeOptions { get; } = new() { 5, 10, 15, 20};

    [ObservableProperty] private int _productPageSize;
    [ObservableProperty] private int _categoryPageSize;
    [ObservableProperty] private int _customerPageSize;
    [ObservableProperty] private int _taxPageSize;
    [ObservableProperty] private int _orderPageSize;

    public SettingsViewModel(SettingService settingService)
    {
        _settingService = settingService;

        _productPageSize = _settingService.GetProductPageSize();
        _categoryPageSize = _settingService.GetCategoryPageSize();
        _customerPageSize = _settingService.GetCustomerPageSize();
        _taxPageSize = _settingService.GetTaxPageSize();
        _orderPageSize = _settingService.GetOrderPageSize();
    }

    partial void OnProductPageSizeChanged(int value) => _settingService.SaveProductPageSize(value);
    partial void OnCategoryPageSizeChanged(int value) => _settingService.SaveCategoryPageSize(value);
    partial void OnCustomerPageSizeChanged(int value) => _settingService.SaveCustomerPageSize(value);
    partial void OnTaxPageSizeChanged(int value) => _settingService.SaveTaxPageSize(value);
    partial void OnOrderPageSizeChanged(int value) => _settingService.SaveOrderPageSize(value);
}