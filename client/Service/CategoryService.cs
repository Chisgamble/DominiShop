using DominiShop.Model;
using DominiShop.Repository;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DominiShop.Service;

public class CategoryService(CategoryRepository categoryRepo, AuthService authService)
{
    private readonly CategoryRepository _repo = categoryRepo;
    private readonly AuthService _auth = authService;

    private int GetPoolId() => _auth.CurrentOwnerId ?? throw new UnauthorizedAccessException("Couldn't identify user.");

    public async Task<(bool Success, List<Category>? Data, string? Error)> GetCategoriesAsync()
    {
        try { var data = await _repo.GetAllByOwnerIdAsync(GetPoolId()); return (true, data, null); }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool Success, Category? Data, string? Error)> CreateCategoryAsync(Category category)
    {
        try { category.OwnerId = GetPoolId(); var res = await _repo.Insert(category); return (true, res, null); }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool Success, string? Error)> UpdateCategoryAsync(Category category)
    {
        try { category.OwnerId = GetPoolId(); var ok = await _repo.UpdateByID(category); return ok ? (true, null) : (false, "Update error."); }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool Success, string? Error)> DeleteCategoryAsync(int id)
    {
        try { var ok = await _repo.DeleteByID(id, GetPoolId()); return ok ? (true, null) : (false, "Delete failed."); }
        catch (Exception ex) { return (false, ex.Message); }
    }
}