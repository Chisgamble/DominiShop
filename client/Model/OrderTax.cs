using System;
using System.Collections.Generic;

namespace DominiShop.Model;

public partial class OrderTax
{
    public long Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? OrderId { get; set; }

    public long? TaxId { get; set; }

    public virtual Order? Order { get; set; }

    public virtual Tax? Tax { get; set; }
}
