using DominiShop.Model;
using DominiShop.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;

namespace DominiShop.Service;

public class AuthService (IRepo<Owner, Guid> ownerRepo)
{
    public Owner? CurrentOwner { get; private set; }
    private readonly IRepo<Owner, Guid> _ownerRepo = ownerRepo;
    public async Task<(bool Success, string? Error)> LoginAsync(string email, string password)
    {
        await Task.Delay(600);

        var all = await _ownerRepo.GetAll();
        var Owner = all.Items.FirstOrDefault(u =>
            string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

        if (Owner is null || Owner.IsDeleted)
            return (false, "No account found with that email.");

        if (Owner.Password != password)
            return (false, "Incorrect password.");

        CurrentOwner = Owner;
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> SignUpAsync(
        string username, string email, string password)
    {
        await Task.Delay(600);

        var all = await _ownerRepo.GetAll();
        var exists = all.Items.Any(u =>
            string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

        if (exists)
            return (false, "An account with this email already exists.");

        await _ownerRepo.Insert(new Owner
        {
            Username = username,
            Email = email,
            Password = password
        });

        return (true, null);
    }

    public Task LogoutAsync()
    {
        CurrentOwner = null;
        return Task.CompletedTask;
    }
}