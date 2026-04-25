using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominiShop.Model;
using DominiShop.Repository;
using DominiShop.Service;
using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace DominiShop.ViewModel
{
    public partial class VoucherViewModel(VoucherService voucherService, AuthService authService) : BaseViewModel
    {
        private readonly VoucherService _service = voucherService;
        private readonly AuthService _authService = authService;

        // states
        [ObservableProperty]
        public partial bool IsLoading { get; set; }
        [ObservableProperty]
        public partial string? ErrorMessage { get; set; }
        [ObservableProperty]
        public partial string? SuccessMessage { get; set; }
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        public bool HasSuccess => !string.IsNullOrEmpty(SuccessMessage);

        // list
        [ObservableProperty]
        public partial ObservableCollection<Voucher> Vouchers { get; set; } = new();

        // pagination states
        [ObservableProperty]
        public partial int CurrentPage { get; set; } = 1;
        [ObservableProperty]
        public partial int TotalPages { get; set; } = 1;
        [ObservableProperty]
        public partial bool HasNext { get; set; }
        [ObservableProperty]
        public partial bool HasPrevious { get; set; }

        private const int PageSize = 10;

        // form fields (create and edit)
        [ObservableProperty]
        public partial Voucher? SelectedVoucher { get; set; }

        [ObservableProperty]
        public partial string FormCode { get; set; } = string.Empty;
        [ObservableProperty]
        public partial string? FormType { get; set; }
        [ObservableProperty]
        public partial string FormPercent { get; set; } = string.Empty;
        [ObservableProperty]
        public partial DateTimeOffset? FormExpiryDate { get; set; }
        [ObservableProperty]
        public partial string FormMaxPerPerson { get; set; } = string.Empty;
        [ObservableProperty]
        public partial bool FormIsActive { get; set; } = true;

        // edit mode check
        [ObservableProperty]
        public partial bool IsEditMode { get; set; }

        // controls whether the form panel is visible
        [ObservableProperty]
        public partial bool IsFormVisible { get; set; } = false;

        // callbacks for property changes
        partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));
        partial void OnSuccessMessageChanged(string? value) => OnPropertyChanged(nameof(HasSuccess));

        // load data
        [RelayCommand]
        public async Task LoadVouchersAsync()
        {
            await RunAsync(async () =>
            {
                var result = await _service.GetVouchersAsync(new PagingRequest
                {
                    PageNumber = CurrentPage,
                    PageSize = PageSize
                });

                Vouchers = new ObservableCollection<Voucher>(result.Items ?? []);
                TotalPages = result.Pagination.TotalPages;
                HasNext = result.Pagination.HasNext;
                HasPrevious = result.Pagination.HasPrevious;
            });
        }

        // pagination
        [RelayCommand]
        public async Task NextPageAsync()
        {
            if (!HasNext) return;
            CurrentPage++;
            await LoadVouchersAsync();
        }

        [RelayCommand]
        public async Task PreviousPageAsync()
        {
            if (!HasPrevious) return;
            CurrentPage--;
            await LoadVouchersAsync();
        }

        // select for edit / prepare create
        [RelayCommand]
        public void SelectForEdit(Voucher voucher)
        {
            SelectedVoucher = voucher;
            IsEditMode = true;
            IsFormVisible = true;
            PopulateForm(voucher);
            ClearMessages();
        }

        [RelayCommand]
        public void PrepareCreate()
        {
            SelectedVoucher = null;
            IsEditMode = false;
            IsFormVisible = true;
            ClearForm();
            ClearMessages();
        }

        [RelayCommand]
        public void CloseForm()
        {
            IsFormVisible = false;
            SelectedVoucher = null;
            IsEditMode = false;
            ClearForm();
            ClearMessages();
        }

        // save (create or update)
        [RelayCommand]
        public async Task SaveAsync()
        {
            if (!TryParseForm(out var percent, out var maxPerPerson, out var expiryDate))
                return;

            if (IsEditMode && SelectedVoucher != null)
            {
                // update voucher
                await RunAsync(async () =>
                {
                    var (ok, err) = await _service.UpdateAsync(
                        SelectedVoucher.Id,
                        FormCode,
                        FormType,
                        percent,
                        expiryDate,
                        maxPerPerson,
                        FormIsActive);

                    if (!ok) { SetError(err); return; }

                    SuccessMessage = "Voucher updated successfully.";
                    await LoadVouchersAsync();
                });
            }
            else
            {
                // create new voucher
                var ownerId = GetCurrentOwnerId();
                if (ownerId == null) { SetError("Could not determine current owner."); return; }

                await RunAsync(async () =>
                {
                    var (ok, err, _) = await _service.CreateAsync(
                        FormCode,
                        FormType,
                        percent,
                        expiryDate,
                        maxPerPerson,
                        ownerId.Value);

                    if (!ok) { SetError(err); return; }

                    SuccessMessage = "Voucher created successfully.";
                    ClearForm();
                    CurrentPage = 1;
                    await LoadVouchersAsync();
                });
            }
        }

        // delete
        [RelayCommand]
        public async Task DeleteAsync(Voucher voucher)
        {
            await RunAsync(async () =>
            {
                var (ok, err) = await _service.DeleteAsync(voucher.Id);
                if (!ok) { SetError(err); return; }

                Vouchers.Remove(voucher);
                if (SelectedVoucher?.Id == voucher.Id)
                {
                    SelectedVoucher = null;
                    IsFormVisible = false;
                    ClearForm();
                }
                SuccessMessage = "Voucher deleted.";
            });
        }

        // helpers
        // try to parse form fields and validate
        private bool TryParseForm(out decimal? percent, out int? maxPerPerson, out DateTime? expiryDate)
        {
            percent = null;
            maxPerPerson = null;
            expiryDate = FormExpiryDate?.UtcDateTime;

            if (!string.IsNullOrWhiteSpace(FormPercent))
            {
                if (!decimal.TryParse(FormPercent, out var p))
                {
                    SetError("Percent must be a valid number.");
                    return false;
                }
                percent = p;
            }

            if (!string.IsNullOrWhiteSpace(FormMaxPerPerson))
            {
                if (!int.TryParse(FormMaxPerPerson, out var m))
                {
                    SetError("Max per person must be a whole number.");
                    return false;
                }
                maxPerPerson = m;
            }

            return true;
        }

        // populate form fields from a voucher
        private void PopulateForm(Voucher v)
        {
            FormCode = v.Code;
            FormType = v.Type;
            FormPercent = v.Percent?.ToString() ?? string.Empty;
            FormExpiryDate = v.ExpiryDate.HasValue
                ? new DateTimeOffset(v.ExpiryDate.Value, TimeSpan.Zero)
                : null;
            FormMaxPerPerson = v.MaxPerPerson?.ToString() ?? string.Empty;
            FormIsActive = v.IsActive;
        }

        private void ClearForm()
        {
            FormCode = string.Empty;
            FormType = null;
            FormPercent = string.Empty;
            FormExpiryDate = null;
            FormMaxPerPerson = string.Empty;
            FormIsActive = true;
        }

        private void ClearMessages()
        {
            ErrorMessage = null;
            SuccessMessage = null;
        }

        private void SetError(string? msg)
        {
            SuccessMessage = null;
            ErrorMessage = msg;
        }

        private int? GetCurrentOwnerId()
        {
            var user = _authService.CurrentUser;
            if (user != null)
            {
                return int.TryParse(user.Id, out var id) ? id : (int?)null;
            }

            return null;
        }

        private async Task RunAsync(Func<Task> action)
        {
            IsLoading = true;
            ClearMessages();
            try { await action(); }
            finally { IsLoading = false; }
        }

        // UI helpers
        public bool IsEmpty => !IsLoading && Vouchers.Count == 0;
        public string FormPanelTitle => IsEditMode ? "Edit Voucher" : "New Voucher";
        public string SaveButtonLabel => IsEditMode ? "Save Changes" : "Create Voucher";
        public string FormPanelColumnWidth => IsFormVisible ? "360" : "0";

        partial void OnIsFormVisibleChanged(bool value)
            => OnPropertyChanged(nameof(FormPanelColumnWidth));

        partial void OnIsLoadingChanged(bool value)
        {
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(SaveButtonLabel));
            OnPropertyChanged(nameof(FormPanelTitle));
        }

        partial void OnVouchersChanged(ObservableCollection<Voucher> value)
            => OnPropertyChanged(nameof(IsEmpty));

        partial void OnIsEditModeChanged(bool value)
        {
            OnPropertyChanged(nameof(FormPanelTitle));
            OnPropertyChanged(nameof(SaveButtonLabel));
        }
    }
}
