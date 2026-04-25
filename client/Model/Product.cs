using System;
using System.Collections.Generic;

namespace DominiShop.Model;

public partial class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    public int Sold { get; set; }

    public string? Note { get; set; }

    public int? CategoryId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? OwnerId { get; set; }

    public bool? IsDeleted { get; set; }

    public decimal BasePrice { get; set; }

    public virtual Category? Category { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual Owner? Owner { get; set; }
}
