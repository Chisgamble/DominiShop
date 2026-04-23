using DominiShop.Model;
using DominiShop.Repository;
using System;
using System.Threading.Tasks;

namespace DominiShop.Service
{
    public class VoucherService(IRepo<Voucher, int> repo)
    {
        private readonly IRepo<Voucher, int> _repo = repo;

        // get
        public async Task<PagedResult<Voucher>> GetVouchersAsync(PagingRequest? paging = null)
        {
            return await _repo.GetAll(paging);
        }

        public async Task<Voucher?> GetByIdAsync(int id)
        {
            return await _repo.GetById(id);
        }

        // create
        public async Task<(bool Success, string? Error, Voucher? Created)> CreateAsync(
            string code,
            string? type,
            decimal? percent,
            DateTime? expiryDate,
            int? maxPerPerson,
            int ownerId)
        {
            var validation = ValidateFields(code, percent, expiryDate);
            if (validation != null)
                return (false, validation, null);

            var voucher = new Voucher
            {
                Code = code.Trim().ToUpperInvariant(),
                Type = type,
                Percent = percent,
                ExpiryDate = expiryDate,
                MaxPerPerson = maxPerPerson,
                IsActive = true,
                OwnerId = ownerId,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                var created = await _repo.Insert(voucher);
                return (true, null, created);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        // update
        public async Task<(bool Success, string? Error)> UpdateAsync(
            int id,
            string code,
            string? type,
            decimal? percent,
            DateTime? expiryDate,
            int? maxPerPerson,
            bool isActive)
        {
            var validation = ValidateFields(code, percent, expiryDate);
            if (validation != null)
                return (false, validation);

            var voucher = new Voucher
            {
                Id = id,
                Code = code.Trim().ToUpperInvariant(),
                Type = type,
                Percent = percent,
                ExpiryDate = expiryDate,
                MaxPerPerson = maxPerPerson,
                IsActive = isActive
            };

            try
            {
                var ok = await _repo.UpdateByID(voucher);
                return ok
                    ? (true, null)
                    : (false, "Voucher not found.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        // delete
        public async Task<(bool Success, string? Error)> DeleteAsync(int id)
        {
            try
            {
                var ok = await _repo.DeleteByID(id);
                return ok
                    ? (true, null)
                    : (false, "Voucher not found.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        // helpers
        private static string? ValidateFields(string code, decimal? percent, DateTime? expiryDate)
        {
            if (string.IsNullOrWhiteSpace(code))
                return "Voucher code is required.";

            if (code.Trim().Length > 50)
                return "Voucher code must be 50 characters or fewer.";

            if (percent.HasValue && (percent < 0 || percent > 100))
                return "Discount percent must be between 0 and 100.";

            if (expiryDate.HasValue && expiryDate.Value.ToUniversalTime() < DateTime.UtcNow)
                return "Expiry date must be in the future.";

            return null;
        }
    }
}
