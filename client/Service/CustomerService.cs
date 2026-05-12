using DominiShop.Model;
using DominiShop.Repository;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DominiShop.Service
{
    public class CustomerService(CustomerRepository customerRepo, AuthService authService)
    {
        private readonly CustomerRepository _repo = customerRepo;
        private readonly AuthService _auth = authService;

        private int GetOwnerId() => _auth.CurrentOwnerId
            ?? throw new UnauthorizedAccessException("Could not determine current owner. Please log in again.");

        public async Task<(bool Success, List<Customer>? Data, string? Error)> GetCustomersAsync()
        {
            try { return (true, await _repo.GetAllByOwnerIdAsync(GetOwnerId()), null); }
            catch (Exception ex) { return (false, null, ex.Message); }
        }

        public async Task<(bool Success, Customer? Data, string? Error)> CreateCustomerAsync(Customer customer)
        {
            try
            {
                var err = ValidateCustomer(customer);
                if (err != null) return (false, null, err);

                customer.OwnerId = GetOwnerId();
                customer.TotalPoints = 0;
                var created = await _repo.Insert(customer);
                return (true, created, null);
            }
            catch (Exception ex) { return (false, null, ex.Message); }
        }

        public async Task<(bool Success, string? Error)> UpdateCustomerAsync(Customer customer)
        {
            try
            {
                var err = ValidateCustomer(customer);
                if (err != null) return (false, err);

                var ok = await _repo.UpdateByPhone(customer);
                return ok ? (true, null) : (false, "Customer not found.");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public async Task<(bool Success, string? Error)> DeleteCustomerAsync(string phone)
        {
            try
            {
                var ok = await _repo.SoftDeleteAsync(phone, GetOwnerId());
                return ok ? (true, null) : (false, "Customer not found.");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public async Task<(bool Success, List<CustomerTier>? Data, string? Error)> GetTiersAsync()
        {
            try { return (true, await _repo.GetAllTiersByOwnerIdAsync(GetOwnerId()), null); }
            catch (Exception ex) { return (false, null, ex.Message); }
        }

        public async Task<(bool Success, CustomerTier? Data, string? Error)> CreateTierAsync(CustomerTier tier)
        {
            try
            {
                var err = ValidateTier(tier);
                if (err != null) return (false, null, err);

                tier.OwnerId = GetOwnerId();
                var created = await _repo.InsertTier(tier);
                return (true, created, null);
            }
            catch (Exception ex) { return (false, null, ex.Message); }
        }

        public async Task<(bool Success, string? Error)> UpdateTierAsync(CustomerTier tier)
        {
            try
            {
                var err = ValidateTier(tier);
                if (err != null) return (false, err);

                var ok = await _repo.UpdateTierById(tier);
                return ok ? (true, null) : (false, "Tier not found.");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public async Task<(bool Success, string? Error)> DeleteTierAsync(int id)
        {
            try
            {
                var ok = await _repo.DeleteTierById(id);
                return ok ? (true, null) : (false, "Tier not found.");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        private static string? ValidateCustomer(Customer c)
        {
            if (string.IsNullOrWhiteSpace(c.Username)) return "Tên khách hàng là bắt buộc.";
            if (c.Username.Trim().Length > 100) return "Tên không được vượt quá 100 ký tự.";
            if (string.IsNullOrWhiteSpace(c.Phone)) return "Số điện thoại là bắt buộc.";
            if (c.Phone.Trim().Length > 20) return "Số điện thoại không hợp lệ.";
            if (string.IsNullOrWhiteSpace(c.Email)) return "Email là bắt buộc.";
            if (!c.Email.Contains('@')) return "Email không hợp lệ.";
            return null;
        }

        private static string? ValidateTier(CustomerTier t)
        {
            if (string.IsNullOrWhiteSpace(t.Name)) return "Tên hạng là bắt buộc.";
            if (t.MinPoint < 0) return "Điểm tối thiểu không được âm.";
            if (t.Percent.HasValue && (t.Percent < 0 || t.Percent > 100))
                return "Phần trăm giảm giá phải từ 0 đến 100.";
            return null;
        }
    }
}