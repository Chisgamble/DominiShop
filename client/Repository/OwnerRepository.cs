using DominiShop.Model;
using DominiShop.Repository;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using DominiShop.DataAccess;

namespace DominiShop.Repository;

public class OwnerRepository : IRepo<Owner, int>
{
    private readonly PostgresContext _context;

    public OwnerRepository(PostgresContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<Owner>> GetAll(PagingRequest? info = null)
    {
        try { 
            info ??= new();
            var query = _context.Owners.Where(o => o.DeletedAt == null);
            var total = await query.CountAsync();

            var items = await query
                    .OrderBy(o => o.Id)
                    .Skip((info.PageNumber - 1) * info.PageSize)
                    .Take(info.PageSize)
                    .ToListAsync();

            return new PagedResult<Owner>
            {
                Items = items,
                Pagination = new PagingMetadata
                {
                    PageNumber = info.PageNumber,
                    PageSize = info.PageSize,
                    TotalItems = total
                }
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Lỗi khi lấy danh sách Owner: {ex.Message}");
        }
    }

    public async Task<Owner?> GetById(int id)
    {
        try {
            return await _context.Owners
                    .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);
        }
        catch (Exception ex)
        {
            throw new Exception($"Lỗi khi tìm Owner theo ID {id}: {ex.Message}");
        }
    }
        

    public async Task<Owner> Insert(Owner item)
    {
        try
        {
            _context.Owners.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }
        catch (DbUpdateException ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;
            throw new Exception($"Lỗi lưu Database: {message}");
        }
    }

    public async Task<bool> UpdateByID(Owner item)
    {
        try
        {
            var existingOwner = await _context.Owners.FindAsync(item.Id);
            if (existingOwner == null || existingOwner.DeletedAt != null) return false;

            existingOwner.Username = item.Username;
            existingOwner.Email = item.Email;
            existingOwner.PasswordHash = item.PasswordHash;
            existingOwner.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;
            throw new Exception($"Lỗi Database khi cập nhật Owner: {message}");
        }
        catch (Exception ex)
        {
            throw new Exception($"Lỗi không xác định khi cập nhật Owner: {ex.Message}");
        }
    }

    public async Task<bool> DeleteByID(int id)
    {
        try
        {
            var owner = await _context.Owners.FindAsync(id);
            if (owner == null) return false;

            owner.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            throw new Exception($"Lỗi khi xóa Owner (ID: {id}): {ex.Message}");
        }
    }


    public async Task<Owner?> GetByEmailAsync(string email)
    {
        try
        {
            // Tìm owner có email trùng khớp và chưa bị xóa
            return await _context.Owners
                    .FirstOrDefaultAsync(o => o.Email == email && o.DeletedAt == null);
        }
        catch (Exception ex)
        {
            throw new Exception($"Lỗi khi tìm Owner theo Email: {ex.Message}");
        }
    }
}