using System;
using System.Collections.Generic;

namespace DominiShop.Model;

public partial class Order
{
    public int Id { get; set; }

    public int? CustomerId { get; set; }

    public int? VoucherId { get; set; }

    public DateTime OrderAt { get; set; }

    public string? Status { get; set; }

    public long? TotalPrice { get; set; }

    public long? ShippingFee { get; set; }

    public string? Address { get; set; }

    public string? PaymentMethod { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? OwnerId { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<OrderVoucher> OrderVouchers { get; set; } = new List<OrderVoucher>();

    public virtual Owner? Owner { get; set; }

    public virtual Voucher? Voucher { get; set; }
}
