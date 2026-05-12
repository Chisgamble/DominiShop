using DominiShop.Model;
using DominiShop.Repository;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DominiShop.Service;

public class OrderService(OrderRepository orderRepo, AuthService authService)
{
    private readonly OrderRepository _repo = orderRepo;
    private readonly AuthService _auth = authService;

    private int GetOwnerId() => _auth.CurrentOwnerId
        ?? throw new UnauthorizedAccessException("Chưa xác định được phiên đăng nhập.");

    public async Task<(bool Success, List<Order>? Data, string? Error)> GetOrdersAsync()
    {
        try { var data = await _repo.GetAllByOwnerIdAsync(GetOwnerId()); return (true, data, null); }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool Success, Order? Data, string? Error)> CreateOrderAsync(Order order)
    {
        try
        {
            if (order.OrderDetails == null || order.OrderDetails.Count == 0)
                return (false, null, "Order must contain at least 1 product.");

            order.OwnerId = GetOwnerId();
            order.Status = "Pending";

            var result = await _repo.Insert(order);
            return (true, result, null);
        }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool Success, string? Error)> UpdateOrderStatusAsync(int orderId, string status)
    {
        try
        {
            var result = await _repo.UpdateStatusAsync(orderId, status);
            return result ? (true, null) : (false, "Order not found");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool Success, Order? Data, string? Error)> UpdateOrderAsync(Order order)
    {
        try
        {
            if (order.OrderDetails == null || order.OrderDetails.Count == 0)
                return (false, null, "Order must contain at least 1 product.");

            order.OwnerId = GetOwnerId();
            var result = await _repo.UpdateOrderAsync(order);
            return (true, result, null);
        }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool Success, string? Error)> DeleteOrderAsync(int orderId)
    {
        try
        {
            var result = await _repo.DeleteOrderAsync(orderId);
            return result ? (true, null) : (false, "Order not found");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }
}
