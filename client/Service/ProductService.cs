using DominiShop.Model;
using DominiShop.Repository;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DominiShop.Service;

public class ProductService(ProductRepository productRepo, AuthService authService)
{
    private readonly ProductRepository _repo = productRepo;
    private readonly AuthService _auth = authService;

    private int GetPoolId() => _auth.CurrentOwnerId ?? throw new UnauthorizedAccessException("Chưa xác định được phiên đăng nhập.");

    public async Task<(bool Success, List<Product>? Data, string? Error)> GetProductsAsync()
    {
        try { var data = await _repo.GetAllByOwnerIdAsync(GetPoolId()); return (true, data, null); }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool Success, Product? Data, string? Error)> CreateProductAsync(Product product)
    {
        try { product.OwnerId = GetPoolId(); var res = await _repo.Insert(product); return (true, res, null); }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool Success, string? Error)> UpdateProductAsync(Product product)
    {
        try { product.OwnerId = GetPoolId(); var ok = await _repo.UpdateByID(product); return ok ? (true, null) : (false, "Lỗi cập nhật sản phẩm."); }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool Success, string? Error)> DeleteProductAsync(int id)
    {
        try { var ok = await _repo.DeleteByID(id, GetPoolId()); return ok ? (true, null) : (false, "Xóa thất bại."); }
        catch (Exception ex) { return (false, ex.Message); }
    }
}