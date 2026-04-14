using System;
using System.Collections.Generic;

namespace DominiShop.Model;

public partial class InventoryLog
{
    public int Id { get; set; }

    public int FoodId { get; set; }

    public int? Quantity { get; set; }

    public string? Type { get; set; }

    public long? TotalPrice { get; set; }

    public string? SourceFrom { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Food Food { get; set; } = null!;
}
