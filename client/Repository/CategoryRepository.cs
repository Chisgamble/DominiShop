using DominiShop.DataAccess;
using DominiShop.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DominiShop.Repository;

public class CategoryRepository : IRepo<Category, int>
{
    private readonly PostgresContext _context;

    public CategoryRepository(PostgresContext context)
    {
        _context = context;
    }

    public async Task<List<Category>> GetAllByOwnerIdAsync(int ownerId)
    {
        try
        {
            return await _context.Categories
                .AsNoTracking()
                .Include(c => c.Products) // Quan trọng để đếm sản phẩm
                .Where(c => c.OwnerId == ownerId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex) { throw new Exception($"Lỗi khi lấy danh sách Category: {ex.Message}"); }
    }

    public async Task<Category?> GetByIdAsync(int id, int ownerId)
    {
        return await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.OwnerId == ownerId);
    }

    public async Task<Category> Insert(Category item)
    {
        try
        {
            item.CreatedAt = DateTime.UtcNow;
            _context.Categories.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }
        catch (DbUpdateException ex) { throw new Exception($"Lỗi lưu Database: {ex.InnerException?.Message ?? ex.Message}"); }
    }

    public async Task<bool> UpdateByID(Category item)
    {
        try
        {
            var existing = await _context.Categories.FirstOrDefaultAsync(c => c.Id == item.Id && c.OwnerId == item.OwnerId);
            if (existing == null) return false;

            existing.Name = item.Name;
            existing.Note = item.Note;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) { throw new Exception($"Lỗi cập nhật Category: {ex.InnerException?.Message ?? ex.Message}"); }
    }

    public async Task<bool> DeleteByID(int id, int ownerId)
    {
        try
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.OwnerId == ownerId);
            if (category == null) return false;

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex) { throw new Exception($"Lỗi khi xóa Category: {ex.Message}"); }
    }

    public Task<PagedResult<Category>> GetAll(PagingRequest? info = null) => throw new NotImplementedException();
    public Task<Category?> GetById(int id) => throw new NotImplementedException();
    public Task<bool> DeleteByID(int id) => throw new NotImplementedException();
}