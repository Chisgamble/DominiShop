using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominiShop.Model;
using DominiShop.Repository;
using DominiShop.Service;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace DominiShop.ViewModel;

public partial class TaxViewModel(TaxService taxService, SettingService settingService) : BaseViewModel
{
    private readonly TaxService _service = taxService;
    private readonly SettingService _settingService = settingService;

    private List<Tax> _masterTaxes = new();
    private List<Tax> _currentFilteredList = new();

    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial Tax? SelectedTax { get; set; }
    [ObservableProperty] public partial Tax EditingTax { get; set; } = new();
    [ObservableProperty] public partial bool IsEditMode { get; set; }

    [ObservableProperty] public partial string? ErrorMessage { get; set; }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));

    [ObservableProperty] public partial bool IsStatusOpen { get; set; }
    [ObservableProperty] public partial string StatusTitle { get; set; } = string.Empty;
    [ObservableProperty] public partial string StatusMessage { get; set; } = string.Empty;
    [ObservableProperty] public partial InfoBarSeverity StatusSeverity { get; set; } = InfoBarSeverity.Informational;

    [ObservableProperty] public partial string SelectedType { get; set; } = "All";
    [ObservableProperty] public partial string SelectedSort { get; set; } = "Latest";
    public List<string> TaxTypes { get; } = new() { "All", "Percentage", "Fixed Amount" };
    public List<string> SortOptions { get; } = new() { "Latest", "Value (High-Low)", "Value (Low-High)" };

    partial void OnSelectedTypeChanged(string value) => FilterData();
    partial void OnSelectedSortChanged(string value) => FilterData();

    [ObservableProperty] public partial int CurrentPage { get; set; } = 1;
    [ObservableProperty] public partial int TotalPages { get; set; } = 1;
    [ObservableProperty] public partial int PageSize { get; set; } = settingService.GetTaxPageSize();

    public List<int> PageSizeOptions { get; } = new() { 5, 10, 15, 20 };
    public bool CanGoPrevious => CurrentPage > 1;
    public bool CanGoNext => CurrentPage < TotalPages;
    public string PagingInfo => $"Page {CurrentPage} of {TotalPages}";

    public ObservableCollection<Tax> Taxes { get; } = new();

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        IsLoading = true;

        PageSize = _settingService.GetTaxPageSize();
        OnPropertyChanged(nameof(PageSize));

        var request = new PagingRequest { PageNumber = 1, PageSize = 10000 };
        var result = await _service.GetTaxesAsync(request, null, null);

        if (result.Success && result.Data?.Items != null)
        {
            _masterTaxes = result.Data.Items.ToList();
            FilterData();
        }
        IsLoading = false;
    }

    private void FilterData()
    {
        var query = _masterTaxes.AsQueryable();

        if (SelectedType != "All")
        {
            query = query.Where(t => t.Type == SelectedType);
        }

        query = SelectedSort switch
        {
            "Value (High-Low)" => query.OrderByDescending(t => t.Value),
            "Value (Low-High)" => query.OrderBy(t => t.Value),
            _ => query.OrderByDescending(t => t.CreatedAt) 
        };

        _currentFilteredList = query.ToList();
        CurrentPage = 1;
        ApplyPaging();
    }

    private void ApplyPaging()
    {
        TotalPages = (int)Math.Ceiling(_currentFilteredList.Count / (double)PageSize);
        if (TotalPages == 0) TotalPages = 1;

        var pagedData = _currentFilteredList
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        Taxes.Clear();
        foreach (var item in pagedData) Taxes.Add(item);

        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(PagingInfo));
    }

    partial void OnPageSizeChanged(int value)
    {
        if (value > 0) _settingService.SaveTaxPageSize(value);
        CurrentPage = 1;
        ApplyPaging();
    }

    [RelayCommand] private void NextPage() { if (CanGoNext) { CurrentPage++; ApplyPaging(); } }
    [RelayCommand] private void PreviousPage() { if (CanGoPrevious) { CurrentPage--; ApplyPaging(); } }


    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(EditingTax.Name)) { ErrorMessage = "Tax name cannot be empty"; return; }
        if (EditingTax.ValueAsDouble < 0) { ErrorMessage = "Tax value cannot be less than 0"; return; }

        IsLoading = true;
        try
        {
            EditingTax.Value = (decimal)EditingTax.ValueAsDouble;
            var res = await _service.SaveTaxAsync(EditingTax);

            if (res.Success)
            {
                IsEditMode = false;
                StatusTitle = "Success";
                StatusMessage = "Operation completed successfully!";
                StatusSeverity = InfoBarSeverity.Success;
                IsStatusOpen = true;
                await LoadDataAsync();
            }
        }
        catch (Exception) { ErrorMessage = "Error : Couldn't save to system"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void AddNew()
    {
        ErrorMessage = null;
        EditingTax = new Tax { Name = string.Empty, Type = "Percentage", Value = 0, AutoApply = false };
        IsEditMode = true;
    }

    [RelayCommand]
    private void Edit(Tax? tax)
    {
        if (tax == null) return;
        EditingTax = new Tax
        {
            Id = tax.Id,
            Name = tax.Name,
            Value = tax.Value,
            Type = tax.Type,
            AutoApply = tax.AutoApply,
            OwnerId = tax.OwnerId,
            CreatedAt = tax.CreatedAt
        };
        IsEditMode = true;
    }

    [RelayCommand]
    public async Task DeleteAsync(Tax? tax)
    {
        if (tax == null) return;
        IsLoading = true;
        if (await _service.DeleteTaxAsync(tax.Id)) await LoadDataAsync();
        IsLoading = false;
    }

    [RelayCommand]
    private async Task ToggleStatus(Tax tax) => await ToggleAutoApplyAsync(tax, !tax.IsAutoApply);

    public async Task ToggleAutoApplyAsync(Tax tax, bool isAutoApply)
    {
        if (tax == null) return;
        var previousValue = tax.AutoApply;
        tax.IsAutoApply = isAutoApply;

        IsLoading = true;
        var result = await _service.SaveTaxAsync(tax);
        if (!result.Success)
        {
            tax.IsAutoApply = previousValue == true;
            StatusTitle = "Error"; StatusMessage = result.Error ?? "Could not update auto apply.";
            StatusSeverity = InfoBarSeverity.Error; IsStatusOpen = true;
        }
        IsLoading = false;
    }
}