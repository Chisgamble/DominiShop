using DominiShop.DataAccess;
using DominiShop.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DominiShop.Repository;

public class OrderRepository
{
    private readonly PostgresContext _context;

    public OrderRepository(PostgresContext context) => _context = context;

    public async Task<List<Order>> GetAllByOwnerIdAsync(int ownerId)
    {
        try
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .Include(o => o.OrderVouchers)
                    .ThenInclude(v => v.Voucher)
                .Include(o => o.OrderTaxes)
                    .ThenInclude(t => t.Tax)
                .Where(o => o.OwnerId == ownerId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex) { throw new Exception($"Failed to load orders: {ex.Message}"); }
    }

    public async Task<Order> Insert(Order order)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                order.CreatedAt = DateTime.UtcNow;
                order.OrderAt = DateTime.UtcNow;

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Update product stock and sold count
                foreach (var detail in order.OrderDetails)
                {
                    var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == detail.ProductId);
                    if (product != null)
                    {
                        product.Quantity -= detail.Quantity;
                        product.Sold += detail.Quantity;
                        product.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return order;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Failed to create order: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Failed to create order: {ex.Message}");
            }
        });
    }
}
