using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominiShop.Model;
using DominiShop.Repository;
using DominiShop.Service;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace DominiShop.ViewModel;

public partial class OrderViewModel : BaseViewModel
{
    private readonly OrderRepository _orderRepo;
    private readonly AuthService _authService;

    // --- State ---
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    // Danh sách order bên trái (master)
    public ObservableCollection<Order> Orders { get; } = new();

    // Order đang được chọn → hiển thị detail bên phải
    [ObservableProperty] public partial Order? SelectedOrder { get; set; }

    // True khi đã chọn order (kiểm soát Visibility panel detail)
    public bool HasSelectedOrder => SelectedOrder != null;

    public OrderViewModel(OrderRepository orderRepo, AuthService authService)
    {
        _orderRepo = orderRepo;
        _authService = authService;
    }

    partial void OnSelectedOrderChanged(Order? value)
        => OnPropertyChanged(nameof(HasSelectedOrder));

    [RelayCommand]
    public async Task LoadOrdersAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            // Lấy ownerId từ session hiện tại
            var ownerEmail = _authService.CurrentUser?.Email;
            if (string.IsNullOrEmpty(ownerEmail))
            {
                ErrorMessage = "Không tìm thấy thông tin đăng nhập.";
                return;
            }

            // Dùng GetAll, filter owner ở repo (hoặc có thể dùng GetByOwnerId nếu truyền ownerId)
            var result = await _orderRepo.GetAll(new PagingRequest { PageSize = 100 });

            Orders.Clear();
            if (result.Items != null)
                foreach (var o in result.Items)
                    Orders.Add(o);

            // Tự chọn dòng đầu nếu có
            SelectedOrder = Orders.FirstOrDefault();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Cập nhật status của order đang chọn
    [RelayCommand]
    public async Task UpdateStatusAsync(string newStatus)
    {
        if (SelectedOrder == null) return;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            SelectedOrder.Status = newStatus;
            await _orderRepo.UpdateByID(SelectedOrder);
            // Notify UI để badge status refresh
            OnPropertyChanged(nameof(SelectedOrder));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
