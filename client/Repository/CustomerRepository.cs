using DominiShop.DataAccess;
using DominiShop.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DominiShop.Repository;

public class CustomerRepository
{
    private readonly PostgresContext _context;

    public CustomerRepository(PostgresContext context) => _context = context;

    public async Task<List<Customer>> GetByOwnerIdAsync(int ownerId)
    {
        try
        {
            return await _context.Customers
                .AsNoTracking()
                .Where(c => c.OwnerId == ownerId && c.DeletedAt == null)
                .OrderBy(c => c.Username)
                .ToListAsync();
        }
        catch (Exception ex) { throw new Exception($"Lỗi lấy danh sách khách hàng: {ex.Message}"); }
    }

    public async Task<List<Customer>> SearchByNameAsync(string name, int ownerId)
    {
        try
        {
            return await _context.Customers
                .AsNoTracking()
                .Where(c => c.OwnerId == ownerId
                         && c.DeletedAt == null
                         && c.Username.Contains(name))
                .OrderBy(c => c.Username)
                .Take(20)
                .ToListAsync();
        }
        catch (Exception ex) { throw new Exception($"Lỗi tìm kiếm khách hàng: {ex.Message}"); }
    }

    public async Task<Customer> Insert(Customer item)
    {
        try
        {
            item.CreatedAt = DateTime.UtcNow;
            _context.Customers.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }
        catch (DbUpdateException ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            if (msg.Contains("customer_email_key") || msg.Contains("unique"))
                throw new Exception($"Email '{item.Email}' đã tồn tại.");
            if (msg.Contains("customer_pkey"))
                throw new Exception($"Số điện thoại '{item.Phone}' đã được đăng ký.");
            throw new Exception($"Lỗi lưu khách hàng: {msg}");
        }
    }
}
