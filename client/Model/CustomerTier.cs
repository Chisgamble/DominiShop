using System;
using System.Collections.Generic;

namespace DominiShop.Model;

public partial class CustomerTier
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public long MinPoint { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
