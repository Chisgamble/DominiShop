using System;
using System.Collections.Generic;

namespace DominiShop.Model;

public partial class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? OwnerId { get; set; }

    public virtual Owner? Owner { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
