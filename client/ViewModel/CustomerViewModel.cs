using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominiShop.Model;
using DominiShop.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace DominiShop.ViewModel
{
    public partial class CustomerViewModel(CustomerService customerService, SettingService settingService) : BaseViewModel
    {
        private readonly CustomerService _service = customerService;
        private readonly SettingService _settingService = settingService;

        private List<Customer> _masterCustomers = new();
        private List<CustomerTier> _masterTiers = new();

        [ObservableProperty] public partial bool IsLoading { get; set; }
        [ObservableProperty] public partial bool IsEditMode { get; set; }
        [ObservableProperty] public partial bool IsTierTab { get; set; } = false;

        [ObservableProperty] public partial int CurrentPage { get; set; } = 1;
        [ObservableProperty] public partial int TotalPages { get; set; } = 1;
        [ObservableProperty] public partial int PageSize { get; set; } = settingService.GetCustomerPageSize();

        public List<int> PageSizeOptions { get; } = new() { 5, 10, 15, 20 };
        public bool CanGoPrevious => CurrentPage > 1;
        public bool CanGoNext => CurrentPage < TotalPages;
        public string PagingInfo => $"Page {CurrentPage} of {TotalPages}";

        // Biến lưu kết quả sau khi lọc (để cắt trang)
        private List<Customer> _currentFilteredList = new();

        public ObservableCollection<Customer> FilteredCustomers { get; } = new();
        public ObservableCollection<CustomerTier> FilteredTiers { get; } = new();

        [ObservableProperty] public partial string SearchText { get; set; } = string.Empty;
        [ObservableProperty] public partial string SelectedSortOption { get; set; } = "Newest";
        [ObservableProperty] public partial string SelectedTierFilter { get; set; } = "All";

        public List<string> SortOptions { get; } = new()
        {
            "Newest", "Oldest", "Name (A-Z)", "Name (Z-A)",
            "Points (High-Low)", "Points (Low-High)"
        };

        public ObservableCollection<string> TierFilterOptions { get; } = new() { "All" };

        partial void OnSearchTextChanged(string value) => FilterData();
        partial void OnSelectedSortOptionChanged(string value) => FilterData();
        partial void OnSelectedTierFilterChanged(string value) => FilterData();


        [ObservableProperty] public partial Customer? SelectedCustomer { get; set; }
        [ObservableProperty] public partial Customer EditingCustomer { get; set; } = new();

        [ObservableProperty] public partial int EditingTierIndex { get; set; } = -1;

        public bool HasSelectedCustomer => SelectedCustomer != null;
        partial void OnSelectedCustomerChanged(Customer? value)
        {
            OnPropertyChanged(nameof(HasSelectedCustomer));
            if (value != null) IsEditMode = false;
        }

        [ObservableProperty] public partial CustomerTier? SelectedTier { get; set; }
        [ObservableProperty] public partial CustomerTier EditingTier { get; set; } = new();
        [ObservableProperty] public partial bool IsTierEditMode { get; set; }

        // For UI NumberBox (decimal ↔ double bridging)
        [ObservableProperty] public partial double EditingTierPercent { get; set; }
        [ObservableProperty] public partial double EditingTierMinPoint { get; set; }

        public bool HasSelectedTier => SelectedTier != null;
        partial void OnSelectedTierChanged(CustomerTier? value)
        {
            OnPropertyChanged(nameof(HasSelectedTier));
            if (value != null) IsTierEditMode = false;
        }

        public ObservableCollection<CustomerTier> TierList { get; } = new();


        [RelayCommand]
        public async Task LoadDataAsync()
        {
            IsLoading = true;

            int savedPageSize = _settingService.GetCustomerPageSize();
            if (PageSize != savedPageSize)
            {
                PageSize = savedPageSize;
            }

            OnPropertyChanged(nameof(PageSize));

            try
            {
                var (csOk, customers, csErr) = await _service.GetCustomersAsync();
                if (!csOk || customers == null)
                {
                    IsLoading = false;
                    return;
                }

                var (tiOk, tiers, tiErr) = await _service.GetTiersAsync();
                _masterTiers = tiOk && tiers != null ? tiers : new();
                _masterCustomers = customers;

                // Rebuild tier filter options
                var currentFilter = SelectedTierFilter;
                TierFilterOptions.Clear();
                TierFilterOptions.Add("All");
                foreach (var t in _masterTiers) TierFilterOptions.Add(t.Name);
                SelectedTierFilter = string.IsNullOrEmpty(currentFilter) || !TierFilterOptions.Contains(currentFilter) ? "All" : currentFilter;

                // Rebuild TierList for ComboBox (with a "no tier" sentinel)
                TierList.Clear();
                TierList.Add(new CustomerTier { Id = -1, Name = "(No tier)" });
                foreach (var t in _masterTiers) TierList.Add(t);

                RebuildTiers();
                FilterData();
            }
            finally { IsLoading = false; }
        }

        private void RebuildTiers()
        {
            FilteredTiers.Clear();
            foreach (var t in _masterTiers) FilteredTiers.Add(t);
        }

        private void FilterData()
        {
            var q = _masterCustomers.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var kw = SearchText.Trim().ToLowerInvariant();
                q = q.Where(c =>
                    c.Username.ToLowerInvariant().Contains(kw) ||
                    c.Phone.Contains(kw) ||
                    c.Email.ToLowerInvariant().Contains(kw));
            }

            // Tier filter
            if (SelectedTierFilter != "All")
            {
                var tier = _masterTiers.FirstOrDefault(t => t.Name == SelectedTierFilter);
                if (tier != null)
                    q = q.Where(c => c.TierId == tier.Id);
                else
                    q = q.Where(c => c.TierId == null);
            }

            // Sort
            q = SelectedSortOption switch
            {
                "Oldest" => q.OrderBy(c => c.CreatedAt),
                "Name (A-Z)" => q.OrderBy(c => c.Username),
                "Name (Z-A)" => q.OrderByDescending(c => c.Username),
                "Points (High-Low)" => q.OrderByDescending(c => c.TotalPoints),
                "Points (Low-High)" => q.OrderBy(c => c.TotalPoints),
                _ => q.OrderByDescending(c => c.CreatedAt)
            };

            // THAY VÌ GÁN TRỰC TIẾP, TA LƯU VÀO BIẾN TẠM VÀ GỌI CẮT TRANG
            _currentFilteredList = q.ToList();
            CurrentPage = 1;
            ApplyPaging();
        }

        [RelayCommand]
        public void AddNew()
        {
            IsEditMode = false;
            EditingCustomer = new Customer();
            EditingTierIndex = 0;
        }

        [RelayCommand]
        public void Edit(Customer? customer)
        {
            if (customer == null) return;
            IsEditMode = true;
            EditingCustomer = new Customer
            {
                Username = customer.Username,
                Phone = customer.Phone,
                Email = customer.Email,
                Address = customer.Address,
                TierId = customer.TierId,
                OwnerId = customer.OwnerId
            };

            var idx = TierList.IndexOf(TierList.FirstOrDefault(t => t.Id == customer.TierId) ?? TierList[0]);
            EditingTierIndex = Math.Max(0, idx);
        }

        [RelayCommand]
        public async Task SaveAsync()
        {
            var selectedTier = (EditingTierIndex >= 0 && EditingTierIndex < TierList.Count)
                ? TierList[EditingTierIndex]
                : null;
            EditingCustomer.TierId = (selectedTier?.Id == -1) ? null : selectedTier?.Id;

            if (IsEditMode)
            {
                var (ok, err) = await _service.UpdateCustomerAsync(EditingCustomer);
                if (ok) await LoadDataAsync();
            }
            else
            {
                var (ok, _, err) = await _service.CreateCustomerAsync(EditingCustomer);
                if (ok) await LoadDataAsync();
            }
        }

        public async Task DeleteAsync(Customer customer)
        {
            var (ok, err) = await _service.DeleteCustomerAsync(customer.Phone);
            if (ok) await LoadDataAsync();
        }

        [RelayCommand]
        public void AddNewTier()
        {
            IsTierEditMode = false;
            EditingTier = new CustomerTier();
            EditingTierPercent = 0;
            EditingTierMinPoint = 0;
        }

        [RelayCommand]
        public void EditTier(CustomerTier? tier)
        {
            if (tier == null) return;
            IsTierEditMode = true;
            EditingTier = new CustomerTier
            {
                Id = tier.Id,
                Name = tier.Name,
                MinPoint = tier.MinPoint,
                Percent = tier.Percent,
                OwnerId = tier.OwnerId
            };
            EditingTierPercent = (double)(tier.Percent ?? 0);
            EditingTierMinPoint = (double)tier.MinPoint;
        }

        [RelayCommand]
        public async Task SaveTierAsync()
        {
            EditingTier.MinPoint = (long)EditingTierMinPoint;
            EditingTier.Percent = EditingTierPercent > 0 ? (decimal)EditingTierPercent : null;

            if (IsTierEditMode)
            {
                var (ok, err) = await _service.UpdateTierAsync(EditingTier);
                if (ok) await LoadDataAsync();
            }
            else
            {
                var (ok, _, err) = await _service.CreateTierAsync(EditingTier);
                if (ok) await LoadDataAsync();
            }
        }

        public async Task DeleteTierAsync(CustomerTier tier)
        {
            var (ok, err) = await _service.DeleteTierAsync(tier.Id);
            if (ok) await LoadDataAsync();
        }

        private void ApplyPaging()
        {
            TotalPages = (int)Math.Ceiling(_currentFilteredList.Count / (double)PageSize);
            if (TotalPages == 0) TotalPages = 1;

            var pagedData = _currentFilteredList
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            FilteredCustomers.Clear();
            foreach (var item in pagedData) FilteredCustomers.Add(item);

            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
            OnPropertyChanged(nameof(PagingInfo));
        }

        partial void OnPageSizeChanged(int value)
        {
            if (value > 0) _settingService.SaveCustomerPageSize(value);
            CurrentPage = 1;
            ApplyPaging();
        }

        [RelayCommand] private void NextPage() { if (CanGoNext) { CurrentPage++; ApplyPaging(); } }
        [RelayCommand] private void PreviousPage() { if (CanGoPrevious) { CurrentPage--; ApplyPaging(); } }

    }
}