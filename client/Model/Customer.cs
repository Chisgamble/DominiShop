using System;
using System.Collections.Generic;

namespace DominiShop.Model;

public partial class Customer
{
    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public long TotalPoints { get; set; }

    public int? TierId { get; set; }

    public string? Address { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int OwnerId { get; set; }

    public virtual Owner Owner { get; set; } = null!;

    public virtual CustomerTier? Tier { get; set; }
}
