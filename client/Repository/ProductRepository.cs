using DominiShop.DataAccess;
using DominiShop.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DominiShop.Repository;

public class ProductRepository : IRepo<Product, int>
{
    private readonly PostgresContext _context;

    public ProductRepository(PostgresContext context)
    {
        _context = context;
    }

    public async Task<List<Product>> GetAllByOwnerIdAsync(int ownerId)
    {
        try
        {
            return await _context.Products
                .AsNoTracking() 
                .Include(p => p.Category) 
                .Where(p => p.OwnerId == ownerId && p.IsDeleted != true) 
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex) { throw new Exception($"Lỗi lấy danh sách Product: {ex.Message}"); }
    }

    public async Task<Product> Insert(Product item)
    {
        try
        {
            item.CreatedAt = DateTime.UtcNow;
            item.IsDeleted = false;
            item.Sold = 0;

            _context.Products.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }
        catch (DbUpdateException ex) { throw new Exception($"Lỗi lưu Database: {ex.InnerException?.Message ?? ex.Message}"); }
    }

    public async Task<bool> UpdateByID(Product item)
    {
        try
        {
            var existing = await _context.Products.FirstOrDefaultAsync(p => p.Id == item.Id && p.OwnerId == item.OwnerId);
            if (existing == null) return false;

            existing.Name = item.Name;
            existing.Quantity = item.Quantity;
            existing.Price = item.Price;
            existing.BasePrice = item.BasePrice;
            existing.Note = item.Note;
            existing.CategoryId = item.CategoryId;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) { throw new Exception($"Lỗi cập nhật Product: {ex.InnerException?.Message ?? ex.Message}"); }
    }

    public async Task<bool> DeleteByID(int id, int ownerId)
    {
        try
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == ownerId);
            if (product == null) return false;

            product.IsDeleted = true;
            product.UpdatedAt = DateTime.UtcNow;


            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex) { throw new Exception($"Lỗi khi xóa Product: {ex.Message}"); }
    }

    public Task<PagedResult<Product>> GetAll(PagingRequest? info = null) => throw new NotImplementedException();
    public Task<Product?> GetById(int id) => throw new NotImplementedException();
    public Task<bool> DeleteByID(int id) => throw new NotImplementedException();
}