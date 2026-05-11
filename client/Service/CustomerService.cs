using DominiShop.Model;
using DominiShop.Repository;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DominiShop.Service;

public class CustomerService(CustomerRepository customerRepo, AuthService authService)
{
    private readonly CustomerRepository _repo = customerRepo;
    private readonly AuthService _auth = authService;

    private int GetOwnerId() => _auth.CurrentOwnerId
        ?? throw new UnauthorizedAccessException("Chưa xác định được phiên đăng nhập.");

    public async Task<(bool Success, List<Customer>? Data, string? Error)> GetCustomersAsync()
    {
        try { var data = await _repo.GetByOwnerIdAsync(GetOwnerId()); return (true, data, null); }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool Success, List<Customer>? Data, string? Error)> SearchCustomersAsync(string name)
    {
        try { var data = await _repo.SearchByNameAsync(name, GetOwnerId()); return (true, data, null); }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool Success, Customer? Data, string? Error)> CreateCustomerAsync(Customer customer)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(customer.Username))
                return (false, null, "Customer name is required.");
            if (string.IsNullOrWhiteSpace(customer.Phone))
                return (false, null, "Phone number is required.");
            if (string.IsNullOrWhiteSpace(customer.Email))
                return (false, null, "Email is required.");

            customer.OwnerId = GetOwnerId();
            // Default password hash for "123456"
            customer.PasswordHash = "123456";
            customer.TotalPoints = 0;

            var result = await _repo.Insert(customer);
            return (true, result, null);
        }
        catch (Exception ex) { return (false, null, ex.Message); }
    }
}
