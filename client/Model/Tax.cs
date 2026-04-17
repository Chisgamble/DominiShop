using System;
using System.Collections.Generic;

namespace DominiShop.Model;

/// <summary>
/// Tax
/// </summary>
public partial class Tax
{
    public long Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? OwnerId { get; set; }

    public string? Name { get; set; }

    public double? Percent { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Owner? Owner { get; set; }
}
