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

        public async Task<PagedResult<Voucher>> GetAll(PagingRequest? info = null)
        {
            try
            {
                info ??= new();
                var query = _context.Vouchers.Where(v => v.OwnerId != null);

                var total = await query.CountAsync();
                var items = await query
                    .OrderByDescending(v => v.CreatedAt)
                    .Skip((info.PageNumber - 1) * info.PageSize)
                    .Take(info.PageSize)
                    .ToListAsync();

                return new PagedResult<Voucher>
                {
                    Items = items,
                    Pagination = new PagingMetadata
                    {
                        PageNumber = info.PageNumber,
                        PageSize = info.PageSize,
                        TotalItems = total,
                        HasNext = (info.PageNumber * info.PageSize) < total,
                        HasPrevious = info.PageNumber > 1
                    }
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching vouchers: {ex.Message}");
            }
        }

        public async Task<Voucher?> GetById(int id)
        {
            try
            {
                return await _context.Vouchers.FirstOrDefaultAsync(v => v.Id == id);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching voucher ID {id}: {ex.Message}");
            }
        }

        public async Task<Voucher?> GetByCode(string code)
        {
            try
            {
                return await _context.Vouchers
                    .FirstOrDefaultAsync(v => v.Code == code);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching voucher by code '{code}': {ex.Message}");
            }
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
                // Unique constraint on Code
                if (msg.Contains("voucher_code_key"))
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
                if (msg.Contains("voucher_code_key"))
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
                // Guard against FK violation (voucher is used in orders)
                if (msg.Contains("foreign key") || msg.Contains("violates"))
                    throw new Exception("Cannot delete this voucher because it is already used in orders.");
                throw new Exception($"Database error deleting voucher: {msg}");
            }
        }
    }

}
