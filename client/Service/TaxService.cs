using DominiShop.Model;
using DominiShop.Repository;
using System;
using System.Threading.Tasks;

namespace DominiShop.Service;

public class TaxService(TaxRepository taxRepo, AuthService authService)
{
    private readonly TaxRepository _repo = taxRepo;
    private readonly AuthService _auth = authService;

    private int GetOwnerId() => _auth.CurrentOwnerId ?? throw new UnauthorizedAccessException("User not logged in.");

    public async Task<(bool Success, PagedResult<Tax>? Data, string? Error)> GetTaxesAsync(PagingRequest paging, string? type, string? sortBy)
    {
        try
        {
            var res = await _repo.GetPagedTaxesAsync(GetOwnerId(), paging, type, sortBy);
            return (true, res, null);
        }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool Success, string? Error)> SaveTaxAsync(Tax tax)
    {
        try
        {
            tax.OwnerId = GetOwnerId();
            if (tax.Id == 0) await _repo.Insert(tax);
            else await _repo.UpdateByID(tax);
            return (true, null);
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<bool> DeleteTaxAsync(long id) => await _repo.DeleteByID(id);
}