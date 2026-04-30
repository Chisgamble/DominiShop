using DominiShop.DataAccess;
using DominiShop.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DominiShop.Repository
{
    public class VoucherRepository(PostgresContext context) : IRepo<Voucher, int>
    {
        private readonly PostgresContext _context = context;

        public async Task<List<Voucher>> GetAllByOwnerIdAsync(int ownerId)
        {
            try
            {
                return await _context.Vouchers
                    .AsNoTracking()
                    .Where(v => v.OwnerId == ownerId)
                    .OrderByDescending(v => v.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex) { throw new Exception($"Error fetching vouchers: {ex.Message}"); }
        }

        public async Task<Voucher?> GetByCodeAsync(string code, int ownerId)
        {
            try
            {
                return await _context.Vouchers
                    .FirstOrDefaultAsync(v => v.Code == code && v.OwnerId == ownerId);
            }
            catch (Exception ex) { throw new Exception($"Error fetching voucher by code: {ex.Message}"); }
        }

        public async Task<Voucher> Insert(Voucher item)
        {
            try
            {
                item.CreatedAt = DateTime.UtcNow;
                _context.Vouchers.Add(item);
                await _context.SaveChangesAsync();
                return item;
            }
            catch (DbUpdateException ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                if (msg.Contains("voucher_code_key") || msg.Contains("unique"))
                    throw new Exception($"Voucher code '{item.Code}' already exists.");
                throw new Exception($"Database error saving voucher: {msg}");
            }
        }

        public async Task<bool> UpdateByID(Voucher item)
        {
            try
            {
                var existing = await _context.Vouchers.FindAsync(item.Id);
                if (existing == null) return false;

                existing.Code = item.Code;
                existing.Type = item.Type;
                existing.Percent = item.Percent;
                existing.ExpiryDate = item.ExpiryDate;
                existing.IsActive = item.IsActive;
                existing.MaxPerPerson = item.MaxPerPerson;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                if (msg.Contains("voucher_code_key") || msg.Contains("unique"))
                    throw new Exception($"Voucher code '{item.Code}' already exists.");
                throw new Exception($"Database error updating voucher: {msg}");
            }
        }

        public async Task<bool> DeleteByID(int id)
        {
            try
            {
                var voucher = await _context.Vouchers.FindAsync(id);
                if (voucher == null) return false;

                _context.Vouchers.Remove(voucher);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                if (msg.Contains("foreign key") || msg.Contains("violates"))
                    throw new Exception("Cannot delete this voucher because it is already used in orders.");
                throw new Exception($"Database error deleting voucher: {msg}");
            }
        }

        // IRepo members not used (owner-scoped methods preferred)
        public Task<PagedResult<Voucher>> GetAll(PagingRequest? info = null) => throw new NotImplementedException();
        public Task<Voucher?> GetById(int id) => throw new NotImplementedException();
    }
}
