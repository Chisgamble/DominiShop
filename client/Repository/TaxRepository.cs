using DominiShop.DataAccess;
using DominiShop.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DominiShop.Repository;

public class TaxRepository : IRepo<Tax, long>
{
    private readonly PostgresContext _context;

    public TaxRepository(PostgresContext context) => _context = context;

    public async Task<PagedResult<Tax>> GetPagedTaxesAsync(int ownerId, PagingRequest paging, string? type, string? sortBy)
    {
        var query = _context.Taxes.Where(t => t.OwnerId == ownerId).AsNoTracking();

        if (!string.IsNullOrEmpty(type))
            query = query.Where(t => t.Type == type);

        query = sortBy switch
        {
            "Value (High-Low)" => query.OrderByDescending(t => t.Value),
            "Value (Low-High)" => query.OrderBy(t => t.Value),
            _ => query.OrderByDescending(t => t.CreatedAt)
        };

        int totalItems = await query.CountAsync();
        var items = await query.Skip((paging.PageNumber - 1) * paging.PageSize)
                             .Take(paging.PageSize)
                             .ToListAsync();

        return new PagedResult<Tax>
        {
            Items = items,
            Pagination = new PagingMetadata
            {
                PageNumber = paging.PageNumber,
                PageSize = paging.PageSize,
                TotalItems = totalItems,
                HasNext = (paging.PageNumber * paging.PageSize) < totalItems,
                HasPrevious = paging.PageNumber > 1
            }
        };
    }

    public async Task<Tax> Insert(Tax item)
    {
        try
        {
            _context.Taxes.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }
        catch (DbUpdateException ex)
        {
            var realError = ex.InnerException?.Message;
            System.Diagnostics.Debug.WriteLine("POSTGRES ERROR: " + realError);
            throw;
        }
    }

    public async Task<bool> UpdateByID(Tax item)
    {
        var existing = await _context.Taxes.FindAsync(item.Id);
        if (existing == null) return false;

        existing.Name = item.Name;
        existing.Value = item.Value;
        existing.Type = item.Type;
        existing.AutoApply = item.AutoApply;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteByID(long id)
    {
        var item = await _context.Taxes.FindAsync(id);
        if (item == null) return false;
        _context.Taxes.Remove(item);
        await _context.SaveChangesAsync();
        return true;
    }

    public Task<PagedResult<Tax>> GetAll(PagingRequest? info = null) => throw new NotImplementedException("Use GetPagedTaxesAsync instead");
    public Task<Tax?> GetById(long id) => _context.Taxes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
}
