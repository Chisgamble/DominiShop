using DominiShop.Model;
using DominiShop.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Supabase;
using Supabase.Gotrue;

namespace DominiShop.Service;

public class AuthService (Supabase.Client supabase, IRepo<Owner, int> _ownerRepo)
{
    public Supabase.Gotrue.Session? CurrentSession => supabase.Auth.CurrentSession;
    public Supabase.Gotrue.User? CurrentUser => supabase.Auth.CurrentUser;

    public int? CurrentOwnerId { get; private set; }

    public async Task<(bool Success, string? Error)> LoginAsync(string email, string password)
    {
        try
        {
            var response = await supabase.Auth.SignIn(email, password);
            if (response != null)
            {
                var ownerRepo = (OwnerRepository)_ownerRepo;
                var owner = await ownerRepo.GetByEmailAsync(email); 
                CurrentOwnerId = owner?.Id;

                return (true, null);
            }
            return (false, "Invalid login attempt.");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool Success, string? Error)> SignUpAsync(string username, string email, string password)
    {
        try
        {
            var signUpOptions = new Supabase.Gotrue.SignUpOptions
            {
                Data = new Dictionary<string, object> { { "username", username } }
            };

            var response = await supabase.Auth.SignUp(email, password, signUpOptions);

            if (response != null)
            {
                //lưu người dùng mới đkí vào bảng Owner
                var newOwner = new Owner
                {
                    Username = username,
                    Email = email,
                    CreatedAt = DateTime.UtcNow
                };

                await _ownerRepo.Insert(newOwner);
                return (true, null);
            }
            return (false, "Sign up failed.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task LogoutAsync() => await supabase.Auth.SignOut();
}