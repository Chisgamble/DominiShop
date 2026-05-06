using DominiShop.DataAccess;
using DominiShop.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DominiShop.Repository;

public class OrderRepository : IRepo<Order, int>
{
    private readonly PostgresContext _context;

    public OrderRepository(PostgresContext context)
    {
        _context = context;
    }

    // Lấy danh sách order theo ownerId, include sẵn OrderDetails + Product để hiển thị master-detail
    public async Task<PagedResult<Order>> GetAll(PagingRequest? info = null)
    {
        try
        {
            info ??= new();
            var query = _context.Orders.AsQueryable();

            var total = await query.CountAsync();

            var items = await query
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .OrderByDescending(o => o.OrderAt)
                .Skip((info.PageNumber - 1) * info.PageSize)
                .Take(info.PageSize)
                .ToListAsync();

            return new PagedResult<Order>
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
            throw new Exception($"Lỗi khi lấy danh sách Order: {ex.Message}");
        }
    }

    // Lấy order theo ownerId để filter đúng cửa hàng
    public async Task<PagedResult<Order>> GetByOwnerId(int ownerId, PagingRequest? info = null)
    {
        try
        {
            info ??= new();
            var query = _context.Orders.Where(o => o.OwnerId == ownerId);

            var total = await query.CountAsync();

            var items = await query
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .OrderByDescending(o => o.OrderAt)
                .Skip((info.PageNumber - 1) * info.PageSize)
                .Take(info.PageSize)
                .ToListAsync();

            return new PagedResult<Order>
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
            throw new Exception($"Lỗi khi lấy Order theo OwnerId: {ex.Message}");
        }
    }

    public async Task<Order?> GetById(int id)
    {
        try
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
        }
        catch (Exception ex)
        {
            throw new Exception($"Lỗi khi tìm Order ID {id}: {ex.Message}");
        }
    }

    public async Task<Order> Insert(Order item)
    {
        try
        {
            _context.Orders.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }
        catch (DbUpdateException ex)
        {
            throw new Exception($"Lỗi lưu Database: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<bool> UpdateByID(Order item)
    {
        try
        {
            var existing = await _context.Orders.FindAsync(item.Id);
            if (existing == null) return false;

            existing.Status = item.Status;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex)
        {
            throw new Exception($"Lỗi Database khi cập nhật Order: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<bool> DeleteByID(int id)
    {
        try
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return false;

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            throw new Exception($"Lỗi khi xóa Order ID {id}: {ex.Message}");
        }
    }
}
