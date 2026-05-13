using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominiShop.Model;
using DominiShop.Repository;
using DominiShop.Service;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace DominiShop.ViewModel;

public partial class TaxViewModel(TaxService taxService) : BaseViewModel
{
    private readonly TaxService _service = taxService;

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

    //Filters & Pagination
    [ObservableProperty] public partial string SelectedType { get; set; } = "All";
    [ObservableProperty] public partial string SelectedSort { get; set; } = "Latest";
    [ObservableProperty] public partial int CurrentPage { get; set; } = 1;
    [ObservableProperty] public partial bool CanGoNext { get; set; }
    [ObservableProperty] public partial bool CanGoPrevious { get; set; }

    public ObservableCollection<Tax> Taxes { get; } = new();
    public List<string> TaxTypes { get; } = new() { "All", "Percentage", "Fixed Amount" };
    public List<string> SortOptions { get; } = new() { "Latest", "Value (High-Low)", "Value (Low-High)" };

    partial void OnSelectedTypeChanged(string value) => LoadDataAfterFilter();
    partial void OnSelectedSortChanged(string value) => LoadDataAfterFilter();

    private void LoadDataAfterFilter()
    {
        CurrentPage = 1;
        LoadDataCommand.Execute(null);
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        IsLoading = true;
        var request = new PagingRequest { PageNumber = CurrentPage, PageSize = 10 };
        var typeFilter = SelectedType == "All" ? null : SelectedType;

        var result = await _service.GetTaxesAsync(request, typeFilter, SelectedSort);

        if (result.Success && result.Data?.Items != null)
        {
            Taxes.Clear();
            foreach (var item in result.Data.Items) Taxes.Add(item);

            CanGoNext = result.Data.Pagination.HasNext;
            CanGoPrevious = result.Data.Pagination.HasPrevious;
        }
        IsLoading = false;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(EditingTax.Name))
        {
            ErrorMessage = "Tax name cannot be empty";
            return;
        }

        if (EditingTax.ValueAsDouble < 0)
        {
            ErrorMessage = "Tax value cannot be less than 0";
            return;
        }

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
                StatusSeverity = InfoBarSeverity.Success; // Màu xanh lá cây
                IsStatusOpen = true;

                await LoadDataAsync();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error : Couldn't save to system";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void AddNew()
    {
        ErrorMessage = null;
        EditingTax = new Tax
        {
            Name = string.Empty,
            Type = "Percentage",
            Value = 0
        };
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
            Value = tax.Value, // decimal
            Type = tax.Type,
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
        var success = await _service.DeleteTaxAsync(tax.Id);
        if (success) await LoadDataAsync();
        IsLoading = false;
    }

    [RelayCommand]
    private async Task ToggleStatus(Tax tax)
    {
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task GoNext()
    {
        if (CanGoNext)
        {
            CurrentPage++;
            await LoadDataAsync();
        }
    }

    [RelayCommand]
    private async Task GoPrevious()
    {
        if (CanGoPrevious)
        {
            CurrentPage--;
            await LoadDataAsync();
        }
    }
}