using DominiShop.Model;
using DominiShop.Repository;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.System;
using System.Linq;

namespace DominiShop.Repository;

public class OwnerRepository : IRepo<Owner, Guid>
{
    private readonly List<Owner> _users =
    [
        new Owner
        {
            Id       = Guid.NewGuid(),
            Username = "DoMini",
            Email    = "admin@gmail.com",
            Password = "admin123",
            Phone    = "0901234567"
        }
    ];

    public Task<PagedResult<Owner>> GetAll(PagingRequest? info = null)
    {
        info ??= new();
        var items = _users
            .Skip((info.PageNumber - 1) * info.PageSize)
            .Take(info.PageSize)
            .ToList();

        return Task.FromResult(new PagedResult<Owner>
        {
            Items = items,
            Pagination = new PagingMetadata
            {
                PageNumber = info.PageNumber,
                PageSize = info.PageSize,
                TotalItems = _users.Count
            }
        });
    }

    public Task<Owner?> GetById(Guid id)
        => Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

    public Task<Owner> Insert(Owner item)
    {
        item.Id = Guid.NewGuid();
        item.CreatedAt = DateTime.Now;
        item.UpdatedAt = DateTime.Now;
        _users.Add(item);
        return Task.FromResult(item);
    }

    public Task<bool> UpdateByID(Owner item)
    {
        var idx = _users.FindIndex(u => u.Id == item.Id);
        if (idx == -1) return Task.FromResult(false);
        item.UpdatedAt = DateTime.Now;
        _users[idx] = item;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteByID(Guid id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        if (user is null) return Task.FromResult(false);
        user.DeletedAt = DateTime.Now;
        return Task.FromResult(true);
    }
}