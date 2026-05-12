using DominiShop.DataAccess;
using DominiShop.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DominiShop.Repository; // Modern file-scoped namespace

public class CustomerRepository(PostgresContext context) // Modern primary constructor
{
    private readonly PostgresContext _context = context;

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

    // From current: Includes Tier and sorts by CreatedAt
    public async Task<List<Customer>> GetAllByOwnerIdAsync(int ownerId)
    {
        try
        {
            return await _context.Customers
                .AsNoTracking()
                .Include(c => c.Tier)
                .Where(c => c.OwnerId == ownerId && c.DeletedAt == null)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex) { throw new Exception($"Error fetching customers: {ex.Message}"); }
    }

    // From incoming: Rescued the search feature and standardized exception language
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
        catch (Exception ex) { throw new Exception($"Error searching customers: {ex.Message}"); }
    }

    // From current
    public async Task<Customer?> GetByPhoneAsync(string phone, int ownerId)
    {
        try
        {
            return await _context.Customers
                .Include(c => c.Tier)
                .FirstOrDefaultAsync(c => c.Phone == phone && c.OwnerId == ownerId && c.DeletedAt == null);
        }
        catch (Exception ex) { throw new Exception($"Error fetching customer: {ex.Message}"); }
    }

    // From current: Kept the robust unique constraint checks
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
                throw new Exception($"Email '{item.Email}' is already registered.");
            if (msg.Contains("customer_pkey"))
                throw new Exception($"Số điện thoại '{item.Phone}' đã được đăng ký.");

            throw new Exception($"Database error saving customer: {msg}");
        }
    }

    public async Task<bool> UpdateByPhone(Customer item)
    {
        try
        {
            var existing = await _context.Customers
                .FirstOrDefaultAsync(c => c.Phone == item.Phone && c.OwnerId == item.OwnerId);
            if (existing == null) return false;

            existing.Username = item.Username;
            existing.Email = item.Email;
            existing.Address = item.Address;
            existing.TierId = item.TierId;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            throw new Exception($"Database error updating customer: {msg}");
        }
    }

    public async Task<bool> SoftDeleteAsync(string phone, int ownerId)
    {
        try
        {
            var existing = await _context.Customers
                .FirstOrDefaultAsync(c => c.Phone == phone && c.OwnerId == ownerId);
            if (existing == null) return false;

            existing.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex) { throw new Exception($"Error deleting customer: {ex.Message}"); }
    }

    public async Task<Customer?> AddPointsAsync(string phone, int ownerId, long points, List<CustomerTier> tiers)
    {
        try
        {
            var customer = await _context.Customers
                .Include(c => c.Tier)
                .FirstOrDefaultAsync(c => c.Phone == phone && c.OwnerId == ownerId && c.DeletedAt == null);
            if (customer == null) return null;

            customer.TotalPoints += points;

            var bestTier = tiers
                .Where(t => t.OwnerId == ownerId && t.MinPoint <= customer.TotalPoints)
                .OrderByDescending(t => t.MinPoint)
                .FirstOrDefault();

            customer.TierId = bestTier?.Id;
            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return customer;
        }
        catch (Exception ex) { throw new Exception($"Error adding points: {ex.Message}"); }
    }

    public async Task<List<CustomerTier>> GetAllTiersByOwnerIdAsync(int ownerId)
    {
        try
        {
            return await _context.CustomerTiers
                .AsNoTracking()
                .Where(t => t.OwnerId == ownerId)
                .OrderBy(t => t.MinPoint)
                .ToListAsync();
        }
        catch (Exception ex) { throw new Exception($"Error fetching customer tiers: {ex.Message}"); }
    }

    public async Task<CustomerTier> InsertTier(CustomerTier item)
    {
        try
        {
            item.CreatedAt = DateTime.UtcNow;
            _context.CustomerTiers.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }
        catch (DbUpdateException ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            throw new Exception($"Database error saving tier: {msg}");
        }
    }

    public async Task<bool> UpdateTierById(CustomerTier item)
    {
        try
        {
            var existing = await _context.CustomerTiers.FindAsync(item.Id);
            if (existing == null) return false;

            existing.Name = item.Name;
            existing.MinPoint = item.MinPoint;
            existing.Percent = item.Percent;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            throw new Exception($"Database error updating tier: {msg}");
        }
    }

    public async Task<bool> DeleteTierById(int id)
    {
        try
        {
            var existing = await _context.CustomerTiers.FindAsync(id);
            if (existing == null) return false;

            _context.CustomerTiers.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex) { throw new Exception($"Error deleting tier: {ex.Message}"); }
    }
}