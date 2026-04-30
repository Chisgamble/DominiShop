using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominiShop.Model;
using DominiShop.Repository;
using DominiShop.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DominiShop.ViewModel
{
    public partial class VoucherViewModel(VoucherService voucherService) : BaseViewModel
    {
        private readonly VoucherService _service = voucherService;
        private List<Voucher> _masterVouchers = new();

        // States
        [ObservableProperty] public partial bool IsLoading { get; set; }
        [ObservableProperty] public partial bool IsEditMode { get; set; }

        // List & filter
        public ObservableCollection<Voucher> FilteredVouchers { get; } = new();
        [ObservableProperty] public partial string SearchText { get; set; } = string.Empty;
        [ObservableProperty] public partial string SelectedSortOption { get; set; } = "Newest";
        [ObservableProperty] public partial string SelectedStatusFilter { get; set; } = "All";
        public List<string> SortOptions { get; } = new()
        {
            "Newest", "Oldest", "Code (A-Z)", "Code (Z-A)",
            "Discount (High-Low)", "Discount (Low-High)", "Expiry (Soonest)"
        };
        public List<string> StatusFilters { get; } = new() { "All", "Active", "Inactive", "Expired" };

        partial void OnSearchTextChanged(string value) => FilterData();
        partial void OnSelectedSortOptionChanged(string value) => FilterData();
        partial void OnSelectedStatusFilterChanged(string value) => FilterData();

        // Selected / editing
        [ObservableProperty] public partial Voucher? SelectedVoucher { get; set; }
        [ObservableProperty] public partial Voucher EditingVoucher { get; set; } = new();

        // Bridging properties for UI controls
        [ObservableProperty] public partial double EditingPercent { get; set; }
        [ObservableProperty] public partial double EditingMaxPerPerson { get; set; }
        [ObservableProperty] public partial DateTimeOffset? EditingExpiryDate { get; set; }
        [ObservableProperty] public partial int EditingTypeIndex { get; set; } = -1;
        [ObservableProperty] public partial bool EditingIsActive { get; set; } = true;

        // Dynamic label and visibility for the discount value field
        // TypeOptions: 0 = percent, 1 = fixed, 2 = free_shipping
        public string DiscountValueHeader => EditingTypeIndex switch
        {
            0 => "Percent discount (1 – 100)",
            1 => "Fixed discount amount (VNĐ)",
            _ => "Discount value"
        };
        public bool IsDiscountValueVisible => EditingTypeIndex == 0 || EditingTypeIndex == 1;

        partial void OnEditingTypeIndexChanged(int value)
        {
            OnPropertyChanged(nameof(DiscountValueHeader));
            OnPropertyChanged(nameof(IsDiscountValueVisible));
        }

        public bool HasSelectedVoucher => SelectedVoucher != null;
        partial void OnSelectedVoucherChanged(Voucher? value)
        {
            OnPropertyChanged(nameof(HasSelectedVoucher));
            if (value != null) IsEditMode = false;
        }

        // Load
        [RelayCommand]
        public async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                var result = await _service.GetVouchersAsync();
                if (result.Success && result.Data != null)
                {
                    _masterVouchers = result.Data;
                    FilterData();
                }
            }
            finally { IsLoading = false; }
        }

        // Filter & sort
        private void FilterData()
        {
            var query = _masterVouchers.AsQueryable();

            // search
            if (!string.IsNullOrWhiteSpace(SearchText))
                query = query.Where(v =>
                    v.Code.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    (v.Type != null && v.Type.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));

            // status filter
            query = SelectedStatusFilter switch
            {
                "Active" => query.Where(v => v.IsActive && !v.IsExpired),
                "Inactive" => query.Where(v => !v.IsActive),
                "Expired" => query.Where(v => v.IsExpired),
                _ => query
            };

            // sort
            query = SelectedSortOption switch
            {
                "Oldest" => query.OrderBy(v => v.CreatedAt),
                "Code (A-Z)" => query.OrderBy(v => v.Code),
                "Code (Z-A)" => query.OrderByDescending(v => v.Code),
                "Discount (High-Low)" => query.OrderByDescending(v => v.Percent),
                "Discount (Low-High)" => query.OrderBy(v => v.Percent),
                "Expiry (Soonest)" => query.OrderBy(v => v.ExpiryDate ?? DateTime.MaxValue),
                _ => query.OrderByDescending(v => v.CreatedAt)
            };

            FilteredVouchers.Clear();
            foreach (var v in query) FilteredVouchers.Add(v);
        }

        // Commands
        [RelayCommand]
        private void AddNew()
        {
            EditingVoucher = new Voucher();
            EditingPercent = 0;
            EditingMaxPerPerson = 0;
            EditingExpiryDate = null;
            EditingTypeIndex = -1;
            EditingIsActive = true;
            IsEditMode = false;  // AddNew = create mode
            SelectedVoucher = null;
        }

        [RelayCommand]
        private void Edit(Voucher? voucher)
        {
            if (voucher == null) return;

            EditingVoucher = new Voucher
            {
                Id = voucher.Id,
                Code = voucher.Code,
                Type = voucher.Type,
                Percent = voucher.Percent,
                ExpiryDate = voucher.ExpiryDate,
                MaxPerPerson = voucher.MaxPerPerson,
                IsActive = voucher.IsActive,
                OwnerId = voucher.OwnerId
            };

            EditingPercent = (double)(voucher.Percent ?? 0);
            EditingMaxPerPerson = voucher.MaxPerPerson ?? 0;
            EditingExpiryDate = voucher.ExpiryDate.HasValue
                ? new DateTimeOffset(voucher.ExpiryDate.Value, TimeSpan.Zero) : null;
            EditingTypeIndex = TypeToIndex(voucher.Type);
            EditingIsActive = voucher.IsActive;
            IsEditMode = true;
        }

        [RelayCommand]
        private void Cancel()
        {
            IsEditMode = false;
            if (EditingVoucher.Id == 0) SelectedVoucher = null;
        }

        [RelayCommand]
        public async Task SaveAsync()
        {
            // Push UI bridge values into the model
            EditingVoucher.Percent = EditingPercent > 0 ? (decimal?)EditingPercent : null;
            EditingVoucher.MaxPerPerson = EditingMaxPerPerson > 0 ? (int?)EditingMaxPerPerson : null;
            EditingVoucher.ExpiryDate = EditingExpiryDate?.UtcDateTime;
            EditingVoucher.Type = IndexToType(EditingTypeIndex);
            EditingVoucher.IsActive = EditingIsActive;

            IsLoading = true;
            bool success;

            if (EditingVoucher.Id == 0)
            {
                var res = await _service.CreateVoucherAsync(EditingVoucher);
                success = res.Success;
            }
            else
            {
                var res = await _service.UpdateVoucherAsync(EditingVoucher);
                success = res.Success;
            }

            if (success)
            {
                IsEditMode = false;
                await LoadDataAsync();
            }

            IsLoading = false;
        }

        [RelayCommand]
        public async Task DeleteAsync(Voucher? voucher)
        {
            if (voucher == null) return;
            IsLoading = true;

            var res = await _service.DeleteVoucherAsync(voucher.Id);
            if (res.Success)
            {
                SelectedVoucher = null;
                IsEditMode = false;
                await LoadDataAsync();
            }

            IsLoading = false;
        }

        // Helpers — indices must match ComboBox item order in VoucherPage.xaml:
        private static readonly string[] TypeOptions = { "percent", "fixed", "free_shipping" };
        private static int TypeToIndex(string? type) => type == null ? -1 : Array.IndexOf(TypeOptions, type);
        private static string? IndexToType(int index) =>
            index >= 0 && index < TypeOptions.Length ? TypeOptions[index] : null;
    }
}