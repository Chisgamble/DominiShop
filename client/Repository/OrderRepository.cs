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

                // Update customer points and tier
                if (!string.IsNullOrEmpty(order.Phone))
                {
                    int totalItems = order.OrderDetails.Sum(d => d.Quantity);
                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == order.Phone && c.OwnerId == order.OwnerId);
                    if (customer != null && totalItems > 0)
                    {
                        customer.TotalPoints += totalItems * 10;
                        
                        var bestTier = await _context.CustomerTiers
                            .Where(t => t.OwnerId == order.OwnerId && t.MinPoint <= customer.TotalPoints)
                            .OrderByDescending(t => t.MinPoint)
                            .FirstOrDefaultAsync();
                            
                        customer.TierId = bestTier?.Id;
                        customer.UpdatedAt = DateTime.UtcNow;
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

    public async Task<bool> UpdateStatusAsync(int orderId, string status)
    {
        try
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order != null)
            {
                order.Status = status;
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
        catch { return false; }
    }

    public async Task<bool> DeleteOrderAsync(int orderId)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .Include(o => o.OrderTaxes)
                    .Include(o => o.OrderVouchers)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null) return false;

                // Only revert stock and points if Pending
                if (order.Status == "Pending")
                {
                    // Revert product stock
                    foreach (var detail in order.OrderDetails)
                    {
                        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == detail.ProductId);
                        if (product != null)
                        {
                            product.Quantity += detail.Quantity;
                            product.Sold -= detail.Quantity;
                        }
                    }

                    // Revert customer points
                    if (!string.IsNullOrEmpty(order.Phone))
                    {
                        int totalItems = order.OrderDetails.Sum(d => d.Quantity);
                        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == order.Phone && c.OwnerId == order.OwnerId);
                        if (customer != null && totalItems > 0)
                        {
                            customer.TotalPoints -= totalItems * 10;
                            if (customer.TotalPoints < 0) customer.TotalPoints = 0;

                            // Re-calculate tier
                            var bestTier = await _context.CustomerTiers
                                .Where(t => t.OwnerId == order.OwnerId && t.MinPoint <= customer.TotalPoints)
                                .OrderByDescending(t => t.MinPoint)
                                .FirstOrDefaultAsync();

                            customer.TierId = bestTier?.Id;
                            customer.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }

                // Explicitly remove child entities first to avoid "severed association" errors
                _context.OrderDetails.RemoveRange(order.OrderDetails);
                _context.OrderTaxes.RemoveRange(order.OrderTaxes);
                _context.OrderVouchers.RemoveRange(order.OrderVouchers);
                _context.Orders.Remove(order);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Failed to delete order: {ex.Message}");
            }
        });
    }

    public async Task<Order> UpdateOrderAsync(Order order)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existing = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .Include(o => o.OrderTaxes)
                    .Include(o => o.OrderVouchers)
                        .ThenInclude(v => v.Voucher)
                    .FirstOrDefaultAsync(o => o.Id == order.Id);

                if (existing == null) throw new Exception("Order not found");

                // 1. Revert old impacts (Regardless of status, to ensure clean baseline for edit)
                foreach (var detail in existing.OrderDetails)
                {
                    var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == detail.ProductId);
                    if (product != null)
                    {
                        product.Quantity += detail.Quantity;
                        product.Sold -= detail.Quantity;
                    }
                }

                if (!string.IsNullOrEmpty(existing.Phone))
                {
                    int oldItems = existing.OrderDetails.Sum(d => d.Quantity);
                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == existing.Phone && c.OwnerId == existing.OwnerId);
                    if (customer != null) customer.TotalPoints -= oldItems * 10;
                }

                // 2. Clear old relations
                _context.OrderDetails.RemoveRange(existing.OrderDetails);
                _context.OrderTaxes.RemoveRange(existing.OrderTaxes);
                _context.OrderVouchers.RemoveRange(existing.OrderVouchers);

                // 3. Update basic info
                existing.Phone = order.Phone;
                existing.TotalPrice = order.TotalPrice;
                existing.ShippingFee = order.ShippingFee;
                existing.Address = order.Address;
                existing.UpdatedAt = DateTime.UtcNow;

                // 4. Add new relations
                foreach (var d in order.OrderDetails) { d.OrderId = existing.Id; d.CreatedAt = DateTime.UtcNow; }
                foreach (var t in order.OrderTaxes) { t.OrderId = existing.Id; t.CreatedAt = DateTime.UtcNow; }
                foreach (var v in order.OrderVouchers) { v.OrderId = existing.Id; v.CreatedAt = DateTime.UtcNow; }

                _context.OrderDetails.AddRange(order.OrderDetails);
                _context.OrderTaxes.AddRange(order.OrderTaxes);
                _context.OrderVouchers.AddRange(order.OrderVouchers);

                // 5. Apply new impacts
                foreach (var detail in order.OrderDetails)
                {
                    var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == detail.ProductId);
                    if (product != null)
                    {
                        product.Quantity -= detail.Quantity;
                        product.Sold += detail.Quantity;
                    }
                }

                if (!string.IsNullOrEmpty(existing.Phone))
                {
                    int newItems = order.OrderDetails.Sum(d => d.Quantity);
                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == existing.Phone && c.OwnerId == existing.OwnerId);
                    if (customer != null)
                    {
                        customer.TotalPoints += newItems * 10;
                        if (customer.TotalPoints < 0) customer.TotalPoints = 0;

                        var bestTier = await _context.CustomerTiers
                            .Where(t => t.OwnerId == existing.OwnerId && t.MinPoint <= customer.TotalPoints)
                            .OrderByDescending(t => t.MinPoint)
                            .FirstOrDefaultAsync();

                        customer.TierId = bestTier?.Id;
                        customer.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return existing;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Failed to update order: {ex.Message}");
            }
        });
    }
}
