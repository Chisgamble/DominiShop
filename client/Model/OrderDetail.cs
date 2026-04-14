using System;
using System.Collections.Generic;

namespace DominiShop.Model;

public partial class OrderDetail
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int? FoodId { get; set; }

    public int Quantity { get; set; }

    public long Price { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Food? Food { get; set; }

    public virtual Order Order { get; set; } = null!;
}
