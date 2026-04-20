using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DominiShop.Repository
{
    public class PagedResult<TData> where TData : class
    {
        public IReadOnlyList<TData>? Items { get; init; }
        public PagingMetadata Pagination { get; set; } = new();
    }

    public class PagingMetadata
    {
        public int PageSize { get; set; } = 10;
        public int PageNumber { get; set; } = 1;
        public int TotalItems { get; set; } = 0;
        public int TotalPages => (int)Math.Ceiling((decimal)TotalItems / PageSize);
        public bool HasNext { get; set; } = false;
        public bool HasPrevious { get; set; } = false;
    }

    public class PagingRequest
    {
        public int PageSize { get; set; } = 10;
        public int PageNumber { get; set; } = 1;
    }

    public interface IRepo<TData, TKey> where TData : class
    {
        Task<PagedResult<TData>> GetAll(PagingRequest? info = null);
        Task<TData?> GetById(TKey id);
        Task<TData> Insert(TData item);
        Task<bool> UpdateByID(TData item);
        Task<bool> DeleteByID(TKey id);
    }
}
