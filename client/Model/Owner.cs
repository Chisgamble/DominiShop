using System;
using System.Collections.Generic;

namespace DominiShop.Model;

public partial class Owner
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

    public virtual ICollection<CustomerTier> CustomerTiers { get; set; } = new List<CustomerTier>();

    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<Tax> Taxes { get; set; } = new List<Tax>();

    public virtual ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();
}
