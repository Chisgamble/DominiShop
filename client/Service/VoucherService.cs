using DominiShop.Model;
using DominiShop.Repository;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DominiShop.Service
{
    public class VoucherService(VoucherRepository voucherRepo, AuthService authService)
    {
        private readonly VoucherRepository _repo = voucherRepo;
        private readonly AuthService _auth = authService;

        private int GetOwnerId() => _auth.CurrentOwnerId
            ?? throw new UnauthorizedAccessException("Could not determine current owner. Please log in again.");

        public async Task<(bool Success, List<Voucher>? Data, string? Error)> GetVouchersAsync()
        {
            try { var data = await _repo.GetAllByOwnerIdAsync(GetOwnerId()); return (true, data, null); }
            catch (Exception ex) { return (false, null, ex.Message); }
        }

        public async Task<(bool Success, Voucher? Data, string? Error)> CreateVoucherAsync(Voucher voucher)
        {
            try
            {
                var err = Validate(voucher);
                if (err != null) return (false, null, err);

                voucher.OwnerId = GetOwnerId();
                voucher.Code = voucher.Code.Trim().ToUpperInvariant();
                voucher.IsActive = true;

                var created = await _repo.Insert(voucher);
                return (true, created, null);
            }
            catch (Exception ex) { return (false, null, ex.Message); }
        }

        public async Task<(bool Success, string? Error)> UpdateVoucherAsync(Voucher voucher)
        {
            try
            {
                var err = Validate(voucher);
                if (err != null) return (false, err);

                voucher.Code = voucher.Code.Trim().ToUpperInvariant();
                var ok = await _repo.UpdateByID(voucher);
                return ok ? (true, null) : (false, "Voucher not found.");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public async Task<(bool Success, string? Error)> DeleteVoucherAsync(int id)
        {
            try
            {
                var ok = await _repo.DeleteByID(id);
                return ok ? (true, null) : (false, "Voucher not found.");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        private static string? Validate(Voucher v)
        {
            if (string.IsNullOrWhiteSpace(v.Code)) return "Voucher code is required.";
            if (v.Code.Trim().Length > 50) return "Voucher code must be 50 characters or fewer.";
            if (v.Percent.HasValue && (v.Percent < 0 || v.Percent > 100))
                return "Discount percent must be between 0 and 100.";
            if (v.ExpiryDate.HasValue && v.ExpiryDate.Value.ToUniversalTime() < DateTime.UtcNow)
                return "Expiry date must be in the future.";
            return null;
        }
    }

}
