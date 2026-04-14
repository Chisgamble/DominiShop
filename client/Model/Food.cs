using System;
using System.Collections.Generic;

namespace DominiShop.Model;

public partial class Food
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Pictures { get; set; } = null!;

    public int Quantity { get; set; }

    public long Price { get; set; }

    public int Sold { get; set; }

    public string? Note { get; set; }

    public int? CategoryId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Category? Category { get; set; }

    public virtual ICollection<InventoryLog> InventoryLogs { get; set; } = new List<InventoryLog>();

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
